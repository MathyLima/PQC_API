package com.pdfController.api.Controller;

import com.pdfController.api.Service.PdfService;
import com.pdfController.api.Service.PdfService.SignatureMetadata;
import com.pdfController.api.Service.PdfService.ValidationException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.*;

import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;

import java.io.FileNotFoundException;
import java.util.HashMap;
import java.util.Map;


@RestController
@RequestMapping("/api/pdfManager")
public class PdfManagerController {

    private static final Logger logger = LoggerFactory.getLogger(PdfManagerController.class);

    @Autowired
    private PdfService pdfService;

    /**
     * Prepares PDF for signing - only calculates hash
     */
    @PostMapping("/preparar")
    public ResponseEntity<ApiResponse<?>> preparar(
            @Valid @RequestBody PrepareRequest request,
            HttpServletRequest httpRequest) {

        try {
            // Auto-populate IP if not provided
            if (request.getMetadata() != null &&
                    (request.getMetadata().getIpAddress() == null ||
                            request.getMetadata().getIpAddress().isEmpty())) {
                request.getMetadata().setIpAddress(getClientIpAddress(httpRequest));
            }

            logger.info("Preparing PDF for signing - Document ID: {}",
                    request.getMetadata() != null ? request.getMetadata().getDocumentId() : "unknown");

            var result = pdfService.prepararPdf(
                    request.getCaminhoArquivo(),
                    request.getMetadata()
            );

            return ResponseEntity.ok(ApiResponse.success(result));

        } catch (ValidationException e) {
            logger.warn("Validation error preparing PDF: {}", e.getMessage());
            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error("Validation error", e.getMessage()));

        } catch (SecurityException e) {
            logger.error("Security violation preparing PDF: {}", e.getMessage());
            return ResponseEntity
                    .status(HttpStatus.FORBIDDEN)
                    .body(ApiResponse.error("Security error", "Access denied"));

        } catch (Exception e) {
            logger.error("Error preparing PDF: {}", e.getMessage(), e);
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("Internal error", "Failed to prepare PDF"));
        }
    }

    /**
     * Finalizes PDF with signature - creates signed PDF directly
     */
    @PostMapping("/finalizar")
    public ResponseEntity<ApiResponse<?>> finalizarPdf(
            @Valid @RequestBody FinalizeRequest request,
            HttpServletRequest httpRequest) {

        try {
            logger.info("Finalizing PDF signature for: {}", sanitizeForLog(request.getCaminhoArquivo()));

            // Auto-populate IP if not provided
            if (request.getMetadata() != null &&
                    (request.getMetadata().getIpAddress() == null ||
                            request.getMetadata().getIpAddress().isEmpty())) {
                request.getMetadata().setIpAddress(getClientIpAddress(httpRequest));
            }

            String signedPath = pdfService.finalizarPdf(
                    request.getCaminhoArquivo(),
                    request.getAssinaturaBase64(),
                    request.getMetadata()
            );

            Map<String, Object> response = new HashMap<>();
            response.put("signedPath", signedPath);
            response.put("message", "PDF assinado com sucesso");

            return ResponseEntity.ok(ApiResponse.success(response));

        } catch (ValidationException e) {
            logger.warn("Validation error finalizing PDF: {}", e.getMessage());
            return ResponseEntity
                    .status(HttpStatus.BAD_REQUEST)
                    .body(ApiResponse.error("Validation error", e.getMessage()));

        } catch (SecurityException e) {
            logger.error("Security violation finalizing PDF: {}", e.getMessage());
            return ResponseEntity
                    .status(HttpStatus.FORBIDDEN)
                    .body(ApiResponse.error("Security error", "Access denied"));

        } catch (Exception e) {
            logger.error("Error finalizing PDF: {}", e.getMessage(), e);
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("Internal error", "Failed to finalize PDF"));
        }
    }

    /**
     * Verifica assinaturas em um PDF
     */
    @GetMapping("/verificar")
    public ResponseEntity<ApiResponse<?>> verificar(@RequestParam String caminhoArquivo) {

        try {
            logger.info("Verifying signatures in PDF: {}", sanitizeForLog(caminhoArquivo));

            var signatures = pdfService.verificarAssinaturas(caminhoArquivo);

            // ✅ LOG: ver exatamente o que está sendo enviado ao C#
            try {
                com.fasterxml.jackson.databind.ObjectMapper mapper =
                        new com.fasterxml.jackson.databind.ObjectMapper();
                mapper.registerModule(new com.fasterxml.jackson.datatype.jsr310.JavaTimeModule());
                String jsonOutput = mapper.writerWithDefaultPrettyPrinter().writeValueAsString(signatures);
                logger.info("=== RESPOSTA ENVIADA AO C# ===\n{}", jsonOutput);
                System.out.println("[VERIFICAR] JSON enviado ao C#:\n" + jsonOutput);
            } catch (Exception logEx) {
                logger.warn("Erro ao logar JSON: {}", logEx.getMessage());
            }

            return ResponseEntity.ok(ApiResponse.success(signatures));

        } catch (FileNotFoundException e) {
            logger.warn("PDF not found: {}", e.getMessage());
            return ResponseEntity
                    .status(HttpStatus.NOT_FOUND)
                    .body(ApiResponse.error("Not found", e.getMessage()));

        } catch (Exception e) {
            logger.error("Error verifying PDF: {}", e.getMessage(), e);
            return ResponseEntity
                    .status(HttpStatus.INTERNAL_SERVER_ERROR)
                    .body(ApiResponse.error("Internal error", "Failed to verify PDF"));
        }
    }
    /**
     * Enhanced IP extraction with security considerations
     */
    private String getClientIpAddress(HttpServletRequest request) {
        String[] headers = {
                "X-Forwarded-For",
                "Proxy-Client-IP",
                "WL-Proxy-Client-IP",
                "HTTP_X_FORWARDED_FOR",
                "HTTP_CLIENT_IP"
        };

        for (String header : headers) {
            String ip = request.getHeader(header);
            if (isValidIp(ip)) {
                if (ip.contains(",")) {
                    ip = ip.split(",")[0].trim();
                }
                return ip;
            }
        }

        String ip = request.getRemoteAddr();
        return ip != null ? ip : "unknown";
    }

    /**
     * Validates IP address format
     */
    private boolean isValidIp(String ip) {
        return ip != null &&
                !ip.isEmpty() &&
                !"unknown".equalsIgnoreCase(ip) &&
                !ip.trim().isEmpty();
    }

    /**
     * Sanitizes string for logging to prevent log injection
     */
    private String sanitizeForLog(String input) {
        if (input == null) return "null";
        return input.replaceAll("[\\r\\n]", "");
    }

    // ========== REQUEST DTOs ==========

    public static class PrepareRequest {
        @NotBlank(message = "Caminho do arquivo é obrigatório")
        private String caminhoArquivo;

        @Valid
        private SignatureMetadata metadata;

        public String getCaminhoArquivo() { return caminhoArquivo; }
        public void setCaminhoArquivo(String caminhoArquivo) { this.caminhoArquivo = caminhoArquivo; }

        public SignatureMetadata getMetadata() { return metadata; }
        public void setMetadata(SignatureMetadata metadata) { this.metadata = metadata; }
    }

    public static class FinalizeRequest {
        @NotBlank(message = "Caminho do arquivo é obrigatório")
        private String caminhoArquivo;

        @NotBlank(message = "Assinatura é obrigatória")
        private String assinaturaBase64;

        @Valid
        private SignatureMetadata metadata;

        public String getCaminhoArquivo() { return caminhoArquivo; }
        public void setCaminhoArquivo(String caminhoArquivo) { this.caminhoArquivo = caminhoArquivo; }

        public String getAssinaturaBase64() { return assinaturaBase64; }
        public void setAssinaturaBase64(String assinaturaBase64) { this.assinaturaBase64 = assinaturaBase64; }

        public SignatureMetadata getMetadata() { return metadata; }
        public void setMetadata(SignatureMetadata metadata) { this.metadata = metadata; }
    }

    // ========== RESPONSE WRAPPER ==========

    public static class ApiResponse<T> {
        private boolean success;
        private T data;
        private ApiError error;
        private long timestamp;

        private ApiResponse(boolean success, T data, ApiError error) {
            this.success = success;
            this.data = data;
            this.error = error;
            this.timestamp = System.currentTimeMillis();
        }

        public static <T> ApiResponse<T> success(T data) {
            return new ApiResponse<>(true, data, null);
        }

        public static <T> ApiResponse<T> error(String type, String message) {
            return new ApiResponse<>(false, null, new ApiError(type, message));
        }

        public boolean isSuccess() { return success; }
        public T getData() { return data; }
        public ApiError getError() { return error; }
        public long getTimestamp() { return timestamp; }
    }

    public static class ApiError {
        private String type;
        private String message;

        public ApiError(String type, String message) {
            this.type = type;
            this.message = message;
        }

        public String getType() { return type; }
        public String getMessage() { return message; }
    }
}