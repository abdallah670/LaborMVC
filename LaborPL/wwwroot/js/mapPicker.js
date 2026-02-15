/**
 * Map Picker Component
 * Uses Leaflet and OpenStreetMap (free, no API key required)
 * Allows users to select their location by clicking on a map
 */
class MapPicker {
    /**
     * Create a new MapPicker instance
     * @param {string} containerId - The ID of the container element
     * @param {Object} options - Configuration options
     * @param {number} options.defaultLat - Default latitude (default: 30.0444 - Cairo)
     * @param {number} options.defaultLng - Default longitude (default: 31.2357 - Cairo)
     * @param {number} options.defaultZoom - Default zoom level (default: 12)
     * @param {Function} options.onLocationChange - Callback when location changes
     */
    constructor(containerId, options = {}) {
        this.containerId = containerId;
        this.options = {
            defaultLat: options.defaultLat || 30.0444,  // Cairo default
            defaultLng: options.defaultLng || 31.2357,
            defaultZoom: options.defaultZoom || 12,
            onLocationChange: options.onLocationChange || function() {},
            markerIcon: options.markerIcon || null
        };
        this.map = null;
        this.marker = null;
        this.isInitialized = false;
        
        this.init();
    }
    
    /**
     * Initialize the Leaflet map
     */
    init() {
        const container = document.getElementById(this.containerId);
        if (!container) {
            console.error(`Map container '${this.containerId}' not found`);
            return;
        }
        
        // Check if Leaflet is available
        if (typeof L === 'undefined') {
            console.error('Leaflet library not loaded. Please include Leaflet CSS and JS.');
            return;
        }
        
        // Initialize Leaflet map
        this.map = L.map(this.containerId, {
            center: [this.options.defaultLat, this.options.defaultLng],
            zoom: this.options.defaultZoom
        });
        
        // Add OpenStreetMap tiles (Free)
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(this.map);
        
        // Create custom marker icon if provided, else use default
        let markerIcon = this.options.markerIcon;
        if (!markerIcon) {
            // Use a custom colored marker
            markerIcon = L.divIcon({
                className: 'custom-marker',
                html: `<div class="marker-pin">
                    <svg viewBox="0 0 24 24" width="32" height="40">
                        <path fill="#dc3545" d="M12 0C7.58 0 4 3.58 4 8c0 5.25 8 13 8 13s8-7.75 8-13c0-4.42-3.58-8-8-8zm0 11c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3z"/>
                    </svg>
                </div>`,
                iconSize: [32, 40],
                iconAnchor: [16, 40],
                popupAnchor: [0, -40]
            });
        }
        
        // Add draggable marker
        this.marker = L.marker(
            [this.options.defaultLat, this.options.defaultLng], 
            { 
                draggable: true,
                icon: markerIcon,
                autoPan: true
            }
        ).addTo(this.map);
        
        // Add click handler to map
        this.map.on('click', (e) => this.onMapClick(e));
        
        // Add drag handler to marker
        this.marker.on('dragend', (e) => this.onMarkerDrag(e));
        
        this.isInitialized = true;
        
        // If we have valid coordinates, trigger initial callback
        if (LocationService.isValidCoordinates(this.options.defaultLat, this.options.defaultLng)) {
            this.notifyLocationChange(this.options.defaultLat, this.options.defaultLng);
        }
    }
    
    /**
     * Handle map click event
     * @param {Object} e - Leaflet click event
     */
    onMapClick(e) {
        const { lat, lng } = e.latlng;
        this.marker.setLatLng(e.latlng);
        this.map.panTo(e.latlng);
        this.notifyLocationChange(lat, lng);
    }
    
    /**
     * Handle marker drag event
     * @param {Object} e - Leaflet drag event
     */
    onMarkerDrag(e) {
        const position = e.target.getLatLng();
        this.map.panTo(position);
        this.notifyLocationChange(position.lat, position.lng);
    }
    
    /**
     * Notify location change callback
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     */
    notifyLocationChange(lat, lng) {
        const data = {
            latitude: lat,
            longitude: lng,
            locationUrl: LocationService.generateGoogleMapsUrl(lat, lng),
            openStreetMapUrl: LocationService.generateOpenStreetMapUrl(lat, lng)
        };
        this.options.onLocationChange(data);
    }
    
    /**
     * Set the map location programmatically
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     * @param {number} zoom - Optional zoom level
     */
    setLocation(lat, lng, zoom = 15) {
        if (!this.isInitialized) return;
        
        this.map.setView([lat, lng], zoom);
        this.marker.setLatLng([lat, lng]);
        this.notifyLocationChange(lat, lng);
    }
    
    /**
     * Get current marker location
     * @returns {Object} {latitude, longitude}
     */
    getLocation() {
        if (!this.isInitialized) return null;
        
        const position = this.marker.getLatLng();
        return {
            latitude: position.lat,
            longitude: position.lng
        };
    }
    
    /**
     * Center map on a location without moving marker
     * @param {number} lat - Latitude
     * @param {number} lng - Longitude
     */
    centerOn(lat, lng, zoom = null) {
        if (!this.isInitialized) return;
        
        if (zoom) {
            this.map.setView([lat, lng], zoom);
        } else {
            this.map.panTo([lat, lng]);
        }
    }
    
    /**
     * Enable/disable marker dragging
     * @param {boolean} enabled - Whether dragging is enabled
     */
    setDraggingEnabled(enabled) {
        if (!this.isInitialized) return;
        this.marker.dragging[enabled ? 'enable' : 'disable']();
    }
    
    /**
     * Add a circle around the marker (useful for accuracy display)
     * @param {number} radius - Radius in meters
     */
    addAccuracyCircle(radius) {
        if (!this.isInitialized) return;
        
        if (this.accuracyCircle) {
            this.map.removeLayer(this.accuracyCircle);
        }
        
        const position = this.marker.getLatLng();
        this.accuracyCircle = L.circle([position.lat, position.lng], {
            radius: radius,
            color: '#4285F4',
            fillColor: '#4285F4',
            fillOpacity: 0.15,
            weight: 1
        }).addTo(this.map);
    }
    
    /**
     * Remove the accuracy circle
     */
    removeAccuracyCircle() {
        if (this.accuracyCircle) {
            this.map.removeLayer(this.accuracyCircle);
            this.accuracyCircle = null;
        }
    }
    
    /**
     * Invalidate map size (call when container is resized or shown)
     */
    invalidateSize() {
        if (this.map) {
            this.map.invalidateSize();
        }
    }
    
    /**
     * Destroy the map instance
     */
    destroy() {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.marker = null;
            this.isInitialized = false;
        }
    }
}

// Make MapPicker available globally
window.MapPicker = MapPicker;