


**Objective**: Implement better error handling, client-side compression, and retry logic for ID verification uploads.

## Current Status

- [x] Root cause identified: Strict FileUploadValidationService blocking ID scans
- [x] Plan approved by user

## Implementation Steps

### [ ] 1. Enhance UploadController error responses (LaborPL/Controllers/UploadController.cs)

- Add `details` object to error JSON: errorCode, fileSize, maxSize, allowedTypes
- Specific messages for signature/size/MIME failures

### [ ] 2. Update IdVerification view (LaborPL/Views/Account/IdVerification.cshtml)

- Client-side image compression using Canvas API (max 2MP, 80% quality)
- Upload progress bar
- Specific error display areas
- Retry buttons per document type
- Preview thumbnails

### [ ] 3. Create verification JavaScript (LaborPL/wwwroot/js/verification.js)

- `compressImage(file)` function
- `uploadIdDocument(file, type)` with retry logic
- `handleUploadError(error)` with user-friendly messages
- `submitVerification()` with form validation

### [ ] 4. Test critical paths

- Upload large JPG/PDF >10MB (should compress)
- Upload invalid signature file
- Rate limit hit
- Full flow: upload → preview → submit → success

### [ ] 5. Verify backend compatibility

- No breaking changes to existing endpoints
- Error responses backward-compatible

## Completion Criteria

- [ ] Upload shows specific errors (not generic)
- [ ] Images auto-compress below limits
- [ ] Retry works after validation preview
- [ ] Submit succeeds with valid URLs
- [ ] User feedback: "ok" or similar

**Next Step**: Implement UploadController improvements
