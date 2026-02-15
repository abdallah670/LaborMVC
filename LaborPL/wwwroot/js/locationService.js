/**
 * Location Service Module
 * Provides geolocation functionality using Browser Geolocation API
 * and reverse geocoding using OpenStreetMap Nominatim (free)
 */
const LocationService = (function() {
    
    /**
     * Check if Geolocation is available in the browser
     * @returns {boolean} True if geolocation is supported
     */
    function isGeolocationAvailable() {
        return 'geolocation' in navigator;
    }
    
    /**
     * Check if the page is served over HTTPS (required for Geolocation API)
     * @returns {boolean} True if page is HTTPS or localhost
     */
    function isSecureContext() {
        return window.location.protocol === 'https:' || 
               window.location.hostname === 'localhost' || 
               window.location.hostname === '127.0.0.1';
    }
    
    /**
     * Get current position using Browser Geolocation API
     * @returns {Promise<Object>} Promise resolving to {latitude, longitude, accuracy}
     */
    function getCurrentPosition() {
        return new Promise((resolve, reject) => {
            if (!isGeolocationAvailable()) {
                reject(new Error('Geolocation is not supported by this browser.'));
                return;
            }
            
            if (!isSecureContext()) {
                reject(new Error('Geolocation requires HTTPS. Please use a secure connection.'));
                return;
            }
            
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    });
                },
                (error) => {
                    switch(error.code) {
                        case error.PERMISSION_DENIED:
                            reject(new Error('Location permission denied. Please allow location access.'));
                            break;
                        case error.POSITION_UNAVAILABLE:
                            reject(new Error('Location information is unavailable. Please try again.'));
                            break;
                        case error.TIMEOUT:
                            reject(new Error('Location request timed out. Please try again.'));
                            break;
                        default:
                            reject(new Error('An unknown error occurred while getting location.'));
                    }
                },
                {
                    enableHighAccuracy: true,
                    timeout: 15000,
                    maximumAge: 0
                }
            );
        });
    }
    
    /**
     * Generate a Google Maps URL from coordinates
     * @param {number} latitude - Geographic latitude
     * @param {number} longitude - Geographic longitude
     * @returns {string} Google Maps URL
     */
    function generateGoogleMapsUrl(latitude, longitude) {
        return `https://www.google.com/maps?q=${latitude},${longitude}`;
    }
    
    /**
     * Generate a Google Maps Embed URL for iframes
     * @param {number} latitude - Geographic latitude
     * @param {number} longitude - Geographic longitude
     * @returns {string} Google Maps Embed URL
     */
    function generateGoogleMapsEmbedUrl(latitude, longitude) {
        return `https://maps.google.com/maps?q=${latitude},${longitude}&z=15&output=embed`;
    }
    
    /**
     * Generate an OpenStreetMap URL from coordinates
     * @param {number} latitude - Geographic latitude
     * @param {number} longitude - Geographic longitude
     * @returns {string} OpenStreetMap URL
     */
    function generateOpenStreetMapUrl(latitude, longitude) {
        return `https://www.openstreetmap.org/?mlat=${latitude}&mlon=${longitude}#map=15/${latitude}/${longitude}`;
    }
    
    /**
     * Reverse geocoding using Nominatim (OpenStreetMap) - Free
     * Note: Rate limited to 1 request per second
     * @param {number} latitude - Geographic latitude
     * @param {number} longitude - Geographic longitude
     * @returns {Promise<Object>} Promise resolving to address data
     */
    async function reverseGeocode(latitude, longitude) {
        try {
            const response = await fetch(
                `https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json`,
                {
                    headers: {
                        'Accept-Language': 'en'
                    }
                }
            );
            
            if (!response.ok) {
                throw new Error('Geocoding service unavailable');
            }
            
            const data = await response.json();
            
            return {
                success: true,
                address: data.display_name || '',
                city: data.address?.city || 
                      data.address?.town || 
                      data.address?.village || 
                      data.address?.municipality || 
                      '',
                state: data.address?.state || data.address?.region || '',
                country: data.address?.country || '',
                postcode: data.address?.postcode || ''
            };
        } catch (error) {
            console.error('Reverse geocoding failed:', error);
            return {
                success: false,
                error: error.message
            };
        }
    }
    
    /**
     * Format coordinates for display
     * @param {number} latitude - Geographic latitude
     * @param {number} longitude - Geographic longitude
     * @param {number} decimals - Number of decimal places (default: 6)
     * @returns {string} Formatted coordinates string
     */
    function formatCoordinates(latitude, longitude, decimals = 6) {
        const lat = latitude.toFixed(decimals);
        const lng = longitude.toFixed(decimals);
        const latDir = latitude >= 0 ? 'N' : 'S';
        const lngDir = longitude >= 0 ? 'E' : 'W';
        return `${Math.abs(lat)}° ${latDir}, ${Math.abs(lng)}° ${lngDir}`;
    }
    
    /**
     * Validate coordinates
     * @param {number} latitude - Geographic latitude
     * @param {number} longitude - Geographic longitude
     * @returns {boolean} True if coordinates are valid
     */
    function isValidCoordinates(latitude, longitude) {
        return !isNaN(latitude) && !isNaN(longitude) &&
               latitude >= -90 && latitude <= 90 &&
               longitude >= -180 && longitude <= 180;
    }
    
    // Public API
    return {
        isGeolocationAvailable,
        isSecureContext,
        getCurrentPosition,
        generateGoogleMapsUrl,
        generateGoogleMapsEmbedUrl,
        generateOpenStreetMapUrl,
        reverseGeocode,
        formatCoordinates,
        isValidCoordinates
    };
})();