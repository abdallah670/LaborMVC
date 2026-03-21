/**
 * ID Verification Upload Module
 * Handles client-side image compression, upload with retry logic, and error handling
 */

(function(window) {
    'use strict';

    // Configuration
    const CONFIG = {
        MAX_FILE_SIZE: 10 * 1024 * 1024, // 10MB
        MAX_IMAGE_DIMENSION: 1920, // Max width/height for compressed images
        MAX_PIXELS: 2000000, // 2MP max for compressed images
        COMPRESSION_QUALITY: 0.8, // 80% quality
        MAX_RETRIES: 3,
        RETRY_DELAY: 2000, // 2 seconds between retries
        ALLOWED_TYPES: ['image/jpeg', 'image/jpg', 'image/png', 'application/pdf'],
        ALLOWED_EXTENSIONS: ['.jpg', '.jpeg', '.png', '.pdf']
    };

    // Upload state tracking
    const uploadState = {
        front: { uploaded: false, url: null, retryCount: 0 },
        back: { uploaded: false, url: null, retryCount: 0 },
        selfie: { uploaded: false, url: null, retryCount: 0 }
    };

    /**
     * Compress an image file using Canvas API
     * @param {File} file - The image file to compress
     * @returns {Promise<Blob>} - Compressed image blob
     */
    async function compressImage(file) {
        return new Promise((resolve, reject) => {
            // If not an image or already small enough, return as-is
            if (!file.type.startsWith('image/') || file.size <= CONFIG.MAX_FILE_SIZE * 0.5) {
                resolve(file);
                return;
            }

            const img = new Image();
            const url = URL.createObjectURL(file);

            img.onload = function() {
                URL.revokeObjectURL(url);

                // Calculate new dimensions while maintaining aspect ratio
                let { width, height } = calculateDimensions(
                    img.width,
                    img.height,
                    CONFIG.MAX_IMAGE_DIMENSION,
                    CONFIG.MAX_PIXELS
                );

                // Create canvas
                const canvas = document.createElement('canvas');
                canvas.width = width;
                canvas.height = height;

                const ctx = canvas.getContext('2d');

                // Use better quality scaling
                ctx.imageSmoothingEnabled = true;
                ctx.imageSmoothingQuality = 'high';

                // Draw image
                ctx.drawImage(img, 0, 0, width, height);

                // Convert to blob
                canvas.toBlob(
                    (blob) => {
                        if (blob) {
                            // Create a new file from the blob
                            const compressedFile = new File(
                                [blob],
                                file.name.replace(/\.[^/.]+$/, '') + '_compressed.jpg',
                                { type: 'image/jpeg', lastModified: Date.now() }
                            );
                            resolve(compressedFile);
                        } else {
                            reject(new Error('Failed to compress image'));
                        }
                    },
                    'image/jpeg',
                    CONFIG.COMPRESSION_QUALITY
                );
            };

            img.onerror = function() {
                URL.revokeObjectURL(url);
                reject(new Error('Failed to load image for compression'));
            };

            img.src = url;
        });
    }

    /**
     * Calculate new dimensions maintaining aspect ratio
     */
    function calculateDimensions(origWidth, origHeight, maxDimension, maxPixels) {
        let width = origWidth;
        let height = origHeight;

        // Scale down if exceeds max dimension
        if (width > maxDimension || height > maxDimension) {
            if (width > height) {
                height = Math.round((height * maxDimension) / width);
                width = maxDimension;
            } else {
                width = Math.round((width * maxDimension) / height);
                height = maxDimension;
            }
        }

        // Check pixel count
        const pixels = width * height;
        if (pixels > maxPixels) {
            const scale = Math.sqrt(maxPixels / pixels);
            width = Math.round(width * scale);
            height = Math.round(height * scale);
        }

        return { width, height };
    }

    /**
     * Upload a single ID document with retry logic
     * @param {File} file - The file to upload
     * @param {string} type - Document type (front, back, selfie)
     * @param {Function} onProgress - Progress callback (loaded, total)
     * @returns {Promise<{success: boolean, url?: string, error?: object}>}
     */
    async function uploadIdDocument(file, type, onProgress = null) {
        const state = uploadState[type];
        state.retryCount = 0;

        while (state.retryCount < CONFIG.MAX_RETRIES) {
            try {
                const result = await attemptUpload(file, type, onProgress);

                if (result.success) {
                    state.uploaded = true;
                    state.url = result.url;
                    return result;
                }

                // If it's a validation error, don't retry
                if (isNonRetryableError(result.error)) {
                    return result;
                }

                // Retry for network/server errors
                state.retryCount++;
                if (state.retryCount < CONFIG.MAX_RETRIES) {
                    await delay(CONFIG.RETRY_DELAY * state.retryCount);
                }
            } catch (error) {
                state.retryCount++;
                if (state.retryCount >= CONFIG.MAX_RETRIES) {
                    return {
                        success: false,
                        error: {
                            errorCode: 'NETWORK_ERROR',
                            message: error.message || 'Network error occurred'
                        }
                    };
                }
                await delay(CONFIG.RETRY_DELAY * state.retryCount);
            }
        }

        return {
            success: false,
            error: {
                errorCode: 'MAX_RETRIES_EXCEEDED',
                message: 'Failed to upload after multiple attempts. Please try again.'
            }
        };
    }

    /**
     * Attempt a single upload
     */
    async function attemptUpload(file, type, onProgress) {
        // First, compress the image if needed
        let processedFile = file;
        try {
            processedFile = await compressImage(file);
        } catch (compressionError) {
            console.warn('Compression failed, using original file:', compressionError);
        }

        // Create form data
        const formData = new FormData();
        formData.append(type === 'front' ? 'frontDocument' : type === 'back' ? 'backDocument' : 'selfie', processedFile);

        // Add anti-forgery token if available
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenElement) {
            formData.append('__RequestVerificationToken', tokenElement.value);
        }

        // Use XMLHttpRequest for progress tracking
        return new Promise((resolve, reject) => {
            const xhr = new XMLHttpRequest();

            // Progress tracking
            if (onProgress) {
                xhr.upload.addEventListener('progress', (e) => {
                    if (e.lengthComputable) {
                        onProgress(e.loaded, e.total);
                    }
                });
            }

            xhr.addEventListener('load', () => {
                if (xhr.status >= 200 && xhr.status < 300) {
                    try {
                        const response = JSON.parse(xhr.responseText);
                        resolve(response);
                    } catch (e) {
                        resolve({ success: true, data: { url: xhr.responseText } });
                    }
                } else {
                    try {
                        const errorResponse = JSON.parse(xhr.responseText);
                        resolve({
                            success: false,
                            error: errorResponse.details || { message: errorResponse.message }
                        });
                    } catch (e) {
                        resolve({
                            success: false,
                            error: { message: `HTTP ${xhr.status}: ${xhr.statusText}` }
                        });
                    }
                }
            });

            xhr.addEventListener('error', () => {
                reject(new Error('Network error'));
            });

            xhr.addEventListener('abort', () => {
                reject(new Error('Upload aborted'));
            });

            xhr.open('POST', '/api/upload/id-documents');
            xhr.send(formData);
        });
    }

    /**
     * Check if error is non-retryable (validation error)
     */
    function isNonRetryableError(error) {
        if (!error) return false;
        const nonRetryableCodes = [
            'SIGNATURE_MISMATCH',
            'INVALID_FILE_TYPE',
            'EXTENSION_NOT_ALLOWED',
            'SECURITY_VIOLATION',
            'FILE_TOO_LARGE'
        ];
        return nonRetryableCodes.includes(error.errorCode);
    }

    /**
     * Delay utility
     */
    function delay(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }

    /**
     * Handle upload errors with user-friendly messages
     * @param {object} error - Error object from upload
     * @returns {object} - User-friendly error info
     */
    function handleUploadError(error) {
        const errorCode = error?.errorCode || 'UNKNOWN_ERROR';
        const details = error?.details || {};

        const errorMessages = {
            'FILE_TOO_LARGE': {
                title: 'File Too Large',
                message: `Your file is ${details.fileSizeFormatted || 'too large'}. Maximum allowed size is ${details.maxSizeFormatted || '10MB'}.`,
                suggestion: 'Please compress your image or use a smaller file.',
                canRetry: false
            },
            'SIGNATURE_MISMATCH': {
                title: 'Invalid File',
                message: 'The file appears to be corrupted or has an incorrect format.',
                suggestion: 'Please check that the file is a valid image and try again.',
                canRetry: false
            },
            'INVALID_FILE_TYPE': {
                title: 'Invalid File Type',
                message: `File type "${details.actualType || 'unknown'}" is not allowed.`,
                suggestion: `Please upload one of these formats: ${(details.allowedTypes || ['JPG', 'PNG', 'PDF']).join(', ')}.`,
                canRetry: false
            },
            'EXTENSION_NOT_ALLOWED': {
                title: 'Invalid File Extension',
                message: `File extension "${details.actualExtension || 'unknown'}" is not allowed.`,
                suggestion: `Please upload files with these extensions: ${(details.allowedTypes || ['.jpg', '.png', '.pdf']).join(', ')}.`,
                canRetry: false
            },
            'RATE_LIMIT_EXCEEDED': {
                title: 'Rate Limit Exceeded',
                message: 'You have uploaded too many files recently.',
                suggestion: 'Please wait a few minutes before trying again.',
                canRetry: true
            },
            'SECURITY_VIOLATION': {
                title: 'Security Check Failed',
                message: 'The file failed security validation.',
                suggestion: 'Please ensure you are uploading a legitimate document image.',
                canRetry: false
            },
            'NETWORK_ERROR': {
                title: 'Network Error',
                message: 'Unable to connect to the server.',
                suggestion: 'Please check your internet connection and try again.',
                canRetry: true
            },
            'MAX_RETRIES_EXCEEDED': {
                title: 'Upload Failed',
                message: 'The upload failed after multiple attempts.',
                suggestion: 'Please try again later or contact support if the problem persists.',
                canRetry: true
            }
        };

        return errorMessages[errorCode] || {
            title: 'Upload Error',
            message: error?.message || 'An unexpected error occurred.',
            suggestion: 'Please try again or contact support if the problem persists.',
            canRetry: true
        };
    }

    /**
     * Validate file before upload
     * @param {File} file - File to validate
     * @returns {object} - Validation result
     */
    function validateFile(file) {
        // Check file exists
        if (!file) {
            return { valid: false, error: 'No file selected' };
        }

        // Check file size
        if (file.size > CONFIG.MAX_FILE_SIZE) {
            return {
                valid: false,
                error: `File size (${formatFileSize(file.size)}) exceeds maximum (${formatFileSize(CONFIG.MAX_FILE_SIZE)})`
            };
        }

        // Check file type
        const extension = '.' + file.name.split('.').pop().toLowerCase();
        if (!CONFIG.ALLOWED_EXTENSIONS.includes(extension)) {
            return {
                valid: false,
                error: `File type "${extension}" not allowed. Allowed: ${CONFIG.ALLOWED_EXTENSIONS.join(', ')}`
            };
        }

        return { valid: true };
    }

    /**
     * Format file size for display
     */
    function formatFileSize(bytes) {
        if (bytes === 0) return '0 Bytes';
        const k = 1024;
        const sizes = ['Bytes', 'KB', 'MB', 'GB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
    }

    /**
     * Reset upload state for a document type
     */
    function resetUploadState(type) {
        uploadState[type] = { uploaded: false, url: null, retryCount: 0 };
    }

    /**
     * Get upload state
     */
    function getUploadState() {
        return { ...uploadState };
    }

    /**
     * Check if all required documents are uploaded
     */
    function areDocumentsReady() {
        return uploadState.front.uploaded;
    }

    /**
     * Get uploaded document URLs
     */
    function getDocumentUrls() {
        return {
            frontUrl: uploadState.front.url,
            backUrl: uploadState.back.url,
            selfieUrl: uploadState.selfie.url
        };
    }

    // Expose public API
    window.IdVerificationUpload = {
        compressImage,
        uploadIdDocument,
        handleUploadError,
        validateFile,
        resetUploadState,
        getUploadState,
        areDocumentsReady,
        getDocumentUrls,
        CONFIG,
        formatFileSize
    };

})(window);
