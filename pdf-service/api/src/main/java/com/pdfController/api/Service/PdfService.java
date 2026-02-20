package com.pdfController.api.Service;

import eu.europa.esig.dss.enumerations.DigestAlgorithm;
import eu.europa.esig.dss.enumerations.SignatureLevel;
import eu.europa.esig.dss.enumerations.SignaturePackaging;
import eu.europa.esig.dss.model.*;
import eu.europa.esig.dss.pades.PAdESSignatureParameters;
import eu.europa.esig.dss.pades.SignatureFieldParameters;
import eu.europa.esig.dss.pades.SignatureImageParameters;
import eu.europa.esig.dss.pades.signature.PAdESService;
import eu.europa.esig.dss.spi.validation.CommonCertificateVerifier;
import eu.europa.esig.dss.pdf.pdfbox.PdfBoxNativeObjectFactory;
import org.apache.pdfbox.Loader;
import org.apache.pdfbox.pdmodel.PDDocument;
import org.apache.pdfbox.pdmodel.PDPage;
import org.apache.pdfbox.pdmodel.PDPageContentStream;
import org.apache.pdfbox.pdmodel.common.PDRectangle;
import org.apache.pdfbox.pdmodel.font.PDType1Font;
import org.apache.pdfbox.pdmodel.font.Standard14Fonts;
import org.springframework.stereotype.Service;

import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.text.SimpleDateFormat;
import java.util.*;

@Service
public class PdfService {

    private static final int MAX_SIGNATURES = 10;
    private static final String PUBLIC_KEY_FIELD_PREFIX = "PQC_PublicKey";

    private static final float PAGE_HEIGHT   = PDRectangle.A4.getHeight();
    private static final float PAGE_WIDTH    = PDRectangle.A4.getWidth();
    private static final float MARGIN        = 40f;
    private static final float FOOTER_HEIGHT = 30f;
    private static final float HEADER_HEIGHT = 60f;
    private static final float USABLE_HEIGHT = PAGE_HEIGHT - MARGIN * 2 - HEADER_HEIGHT - FOOTER_HEIGHT;
    private static final float BLOCK_HEIGHT  = USABLE_HEIGHT / MAX_SIGNATURES;

    // =========================
    // PREPARAR PDF
    // =========================

    public PrepareResponse prepararPdf(String inputPath, SignatureMetadata metadata) throws Exception {

        validateInputPath(inputPath);
        if (metadata != null) validateMetadata(metadata);

        File inputFile = new File(inputPath);
        int existingSignatures = countExistingSignatures(inputFile);
        int nextSignatureIndex = existingSignatures + 1;

        if (nextSignatureIndex > MAX_SIGNATURES)
            throw new ValidationException(
                    "Limite de " + MAX_SIGNATURES + " assinaturas atingido neste documento.");

        String preparedPath = inputPath.replace(".pdf", "_prepared.pdf");

        log("PREPARANDO PDF | assinatura #" + nextSignatureIndex);
        log("Input : " + inputPath + " (" + inputFile.length() + " bytes)");
        log("Output: " + preparedPath);

        Path tempPath = Files.createTempFile("pdf_sign_", ".pdf");

        if (nextSignatureIndex == 1) {
            // ══════════════════════════════════════════════════════════════
            // PRIMEIRA ASSINATURA:
            // Cria a página de metadados completa com TODOS os 10 blocos
            // reservados (vazios), injeta a chave pública da assinatura #1,
            // e salva o PDF com save() normal (pode reescrever, não há
            // assinaturas anteriores para proteger).
            // ══════════════════════════════════════════════════════════════
            log("Primeira assinatura — criando página de metadados completa com " + MAX_SIGNATURES + " blocos.");

            try (PDDocument doc = Loader.loadPDF(inputFile)) {

                // Injeta chave pública da assinatura #1
                if (metadata != null && metadata.getPublicKey() != null
                        && !metadata.getPublicKey().isBlank()) {
                    injectPublicKeyIntoDoc(doc, metadata.getPublicKey(), 1);
                    log("Chave pública #1 injetada.");
                }

                // Cria a página de metadados com todos os 10 blocos pré-desenhados
                // (os blocos 2..10 ficam vazios/invisíveis até serem preenchidos)
                addFullMetadataPageWithAllBlocks(doc, metadata, 1);
                log("Página de metadados com " + MAX_SIGNATURES + " blocos criada.");

                try (OutputStream os = new FileOutputStream(tempPath.toFile())) {
                    doc.save(os);
                }
            }

        } else {
            // ══════════════════════════════════════════════════════════════
            // SEGUNDA ASSINATURA EM DIANTE:
            // Não modifica a estrutura do PDF — apenas preenche o bloco
            // visual e injeta a chave pública via saveIncremental, que
            // APPENDA ao final sem tocar nos bytes anteriores.
            // As assinaturas anteriores continuam válidas.
            // ══════════════════════════════════════════════════════════════
            log("Assinatura #" + nextSignatureIndex + " — salvando incrementalmente.");

            // PDFBox requer arquivo em disco para saveIncremental.
            // Salvamos o PDF original em um temp file e carregamos dele.
            Path incrementalTemp = Files.createTempFile("pdf_inc_", ".pdf");
            Files.copy(inputFile.toPath(), incrementalTemp, StandardCopyOption.REPLACE_EXISTING);

            try (PDDocument doc = Loader.loadPDF(incrementalTemp.toFile())) {

                // Injeta chave pública da nova assinatura
                if (metadata != null && metadata.getPublicKey() != null
                        && !metadata.getPublicKey().isBlank()) {
                    injectPublicKeyIntoDoc(doc, metadata.getPublicKey(), nextSignatureIndex);
                    log("Chave pública #" + nextSignatureIndex + " injetada.");
                }

                // Preenche o bloco visual da nova assinatura na página de metadados
                fillSignatureBlockInPage(doc, metadata, nextSignatureIndex);
                log("Bloco #" + nextSignatureIndex + " preenchido na página de metadados.");

                // ✅ saveIncremental: appenda ao final sem tocar nos bytes anteriores
                try (OutputStream incrementalOut = new FileOutputStream(tempPath.toFile())) {
                    doc.saveIncremental(incrementalOut);
                }

                log("Salvo incrementalmente: " + tempPath.toFile().length() + " bytes");
            } finally {
                Files.deleteIfExists(incrementalTemp);
            }
        }

        log("Arquivo temp: " + tempPath.toFile().length() + " bytes");

        // DSS: usar placeholder para obter ByteRange real
        PAdESSignatureParameters params = buildSignatureParameters(metadata, nextSignatureIndex);
        DSSDocument dssDoc   = new FileDocument(tempPath.toFile());
        PAdESService service = buildPAdESService();

        service.getDataToSign(dssDoc, params); // prepara internamente

        SignatureValue placeholderSig = new SignatureValue();
        placeholderSig.setAlgorithm(params.getSignatureAlgorithm());
        placeholderSig.setValue(new byte[4627]); // tamanho ML-DSA-87 (maior)

        DSSDocument preparedDssDoc = service.signDocument(dssDoc, params, placeholderSig);

        ByteArrayOutputStream preparedBaos = new ByteArrayOutputStream();
        preparedDssDoc.writeTo(preparedBaos);
        byte[] tempBytes = preparedBaos.toByteArray();

        // Extrai o ByteRange da ÚLTIMA assinatura (a recém-adicionada)
        int[] byteRange = extractLastByteRangeFromPdf(tempBytes);

        if (byteRange == null)
            throw new Exception("Não foi possível extrair ByteRange do PDF preparado.");

        log("ByteRange extraído: [" + byteRange[0] + ", " + byteRange[1]
                + ", " + byteRange[2] + ", " + byteRange[3] + "]");

        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        baos.write(tempBytes, byteRange[0], byteRange[1]);
        baos.write(tempBytes, byteRange[2], byteRange[3]);
        byte[] byteRangeContent = baos.toByteArray();

        log("ByteRange content: " + byteRangeContent.length + " bytes");

        byte[] hashToSign = MessageDigest.getInstance("SHA-256").digest(byteRangeContent);
        String hashBase64 = Base64.getEncoder().encodeToString(hashToSign);

        log("Hash SHA-256 (toBeSignedBase64): " + hashBase64);

        try (OutputStream os = new FileOutputStream(preparedPath)) {
            os.write(tempBytes);
        }
        Files.delete(tempPath);

        saveParamsCache(preparedPath, params, byteRangeContent, metadata);

        log("PREPARAÇÃO CONCLUÍDA → " + preparedPath
                + " (" + new File(preparedPath).length() + " bytes)");

        return new PrepareResponse(
                hashToSign,
                hashBase64,
                preparedPath,
                inputFile.getName(),
                metadata,
                nextSignatureIndex
        );
    }

    // =========================
    // FINALIZAR PDF
    // =========================

    public String finalizarPdf(String preparedPath, String signatureBase64,
                               SignatureMetadata metadata) throws Exception {

        log("FINALIZANDO PDF | " + preparedPath);

        if (preparedPath == null || preparedPath.isBlank())
            throw new ValidationException("Caminho do arquivo não pode ser vazio");
        if (signatureBase64 == null || signatureBase64.isBlank())
            throw new ValidationException("Assinatura não pode ser vazia");

        byte[] signatureBytes;
        try {
            signatureBytes = Base64.getDecoder().decode(signatureBase64);
            log("Assinatura decodificada: " + signatureBytes.length + " bytes");
        } catch (IllegalArgumentException e) {
            throw new ValidationException("Assinatura Base64 inválida");
        }

        loadParamsCache(preparedPath); // valida que o cache existe

        byte[] preparedBytes = Files.readAllBytes(Paths.get(preparedPath));

        // Extrai o ByteRange da ÚLTIMA assinatura (a do placeholder)
        int[] byteRange = extractLastByteRangeFromPdf(preparedBytes);

        if (byteRange == null)
            throw new ValidationException("ByteRange não encontrado no PDF preparado.");

        log("ByteRange: [" + byteRange[0] + ", " + byteRange[1]
                + ", " + byteRange[2] + ", " + byteRange[3] + "]");

        int contentsStart     = byteRange[1];
        int contentsEnd       = byteRange[2];
        int contentsFieldSize = contentsEnd - contentsStart;

        StringBuilder hexSig = new StringBuilder();
        for (byte b : signatureBytes)
            hexSig.append(String.format("%02x", b));
        String hexStr = hexSig.toString();

        int availableHexChars = contentsFieldSize - 2;

        if (hexStr.length() > availableHexChars)
            throw new ValidationException(
                    "Assinatura muito grande. Hex: " + hexStr.length()
                            + ", disponível: " + availableHexChars);

        // Pad com zeros à direita
        while (hexStr.length() < availableHexChars)
            hexStr += "0";

        byte[] signedBytes = Arrays.copyOf(preparedBytes, preparedBytes.length);
        signedBytes[contentsStart] = '<';
        byte[] hexBytes = hexStr.getBytes(java.nio.charset.StandardCharsets.US_ASCII);
        System.arraycopy(hexBytes, 0, signedBytes, contentsStart + 1, hexBytes.length);
        signedBytes[contentsEnd - 1] = '>';

        log("Assinatura inserida: " + signatureBytes.length + " bytes → "
                + hexStr.length() + " hex chars");

        String signedPath = preparedPath.replace("_prepared.pdf", "_signed.pdf");
        if (signedPath.equals(preparedPath))
            signedPath = preparedPath.replace(".pdf", "_signed.pdf");

        try (OutputStream os = new FileOutputStream(signedPath)) {
            os.write(signedBytes);
        }

        log("PDF ASSINADO → " + signedPath + " (" + new File(signedPath).length() + " bytes)");

        deleteParamsCache(preparedPath);
        return signedPath;
    }

    // =========================
    // VERIFICAR ASSINATURAS
    // =========================

    public List<SignatureInfo> verificarAssinaturas(String pdfPath) throws Exception {
        log("VERIFICANDO ASSINATURAS | " + pdfPath);

        File file = new File(pdfPath);
        if (!file.exists())
            throw new FileNotFoundException("PDF não encontrado: " + pdfPath);

        List<SignatureInfo> result = new ArrayList<>();
        byte[] pdfBytes = Files.readAllBytes(file.toPath());

        try (PDDocument document = Loader.loadPDF(file)) {
            var sigs = document.getSignatureDictionaries();
            log("Assinaturas encontradas: " + sigs.size());

            for (int i = 0; i < sigs.size(); i++) {
                var sig  = sigs.get(i);
                var info = new SignatureInfo();

                info.setIndex(i + 1);
                info.setName(sig.getName());
                info.setReason(sig.getReason());
                info.setLocation(sig.getLocation());
                info.setSignDate(sig.getSignDate() != null ? sig.getSignDate().getTime() : null);
                info.setFilter(sig.getFilter());
                info.setSubFilter(sig.getSubFilter());

                int[] byteRange = sig.getByteRange();
                if (byteRange != null && byteRange.length == 4) {
                    info.setByteRange(byteRange);
                    info.setByteRangeValid(
                            byteRange[0] == 0 && byteRange[1] > 0 &&
                                    byteRange[2] > byteRange[1] && byteRange[3] > 0);

                    int end = byteRange[2] + byteRange[3];
                    if (end > pdfBytes.length) {
                        log("WARN byterange #" + (i + 1) + ": end=" + end
                                + " > fileSize=" + pdfBytes.length);
                    } else {
                        // ✅ Bytes cobertos por ESTA assinatura (excluindo /Contents)
                        // O PDFBox retorna o ByteRange específico de cada assinatura,
                        // que aponta para os bytes tal como estavam quando foi assinado.
                        ByteArrayOutputStream byteRangeBaos = new ByteArrayOutputStream();
                        byteRangeBaos.write(pdfBytes, byteRange[0], byteRange[1]);
                        byteRangeBaos.write(pdfBytes, byteRange[2], byteRange[3]);
                        byte[] byteRangeContent = byteRangeBaos.toByteArray();

                        try {
                            byte[] rawHash = MessageDigest.getInstance("SHA-256")
                                    .digest(byteRangeContent);
                            info.setByteRangeHashBase64(Base64.getEncoder().encodeToString(rawHash));
                        } catch (Exception e) {
                            log("WARN hash byterange #" + (i + 1) + ": " + e.getMessage());
                        }

                        // ✅ toBeSignedBase64 = SHA-256(bytesDoByteRange desta assinatura)
                        try {
                            byte[] hashToVerify = MessageDigest.getInstance("SHA-256")
                                    .digest(byteRangeContent);
                            String toBeSignedBase64 = Base64.getEncoder().encodeToString(hashToVerify);
                            info.setToBeSignedBase64(toBeSignedBase64);
                            log("toBeSignedBase64 #" + (i + 1) + ": "
                                    + toBeSignedBase64.substring(0, 20) + "..."
                                    + " | conteúdo: " + byteRangeContent.length + " bytes");
                        } catch (Exception e) {
                            log("WARN toBeSignedBase64 #" + (i + 1) + ": " + e.getMessage());
                        }
                    }
                }

                byte[] contents = sig.getContents();
                if (contents != null) {
                    info.setSignatureBase64(Base64.getEncoder().encodeToString(contents));
                    info.setSignatureSize(contents.length);
                }

                try {
                    var acroForm = document.getDocumentCatalog().getAcroForm();
                    if (acroForm != null) {
                        String fieldName = PUBLIC_KEY_FIELD_PREFIX + "_" + (i + 1);
                        var field = acroForm.getField(fieldName);
                        if (field instanceof org.apache.pdfbox.pdmodel.interactive.form.PDTextField tf) {
                            String pk = tf.getValue();
                            if (pk != null && !pk.isBlank()) {
                                info.setPublicKeyBase64(pk);
                                log("Chave pública extraída [" + fieldName + "]: "
                                        + pk.substring(0, Math.min(20, pk.length())) + "...");
                            }
                        }
                    }
                } catch (Exception e) {
                    log("WARN chave pública #" + (i + 1) + ": " + e.getMessage());
                }

                result.add(info);
            }
        }

        return result;
    }

    // =========================
    // PÁGINA DE METADADOS — CRIAÇÃO COMPLETA (1ª assinatura)
    // Desenha TODOS os 10 blocos de uma vez.
    // Os blocos sem metadata ficam com bordas visíveis mas vazios.
    // =========================

    private void addFullMetadataPageWithAllBlocks(PDDocument doc, SignatureMetadata firstMetadata,
                                                  int firstSignatureIndex) throws IOException {
        PDPage page = new PDPage(new PDRectangle(PAGE_WIDTH, PAGE_HEIGHT));
        doc.addPage(page);

        try (PDPageContentStream cs = new PDPageContentStream(
                doc, page, PDPageContentStream.AppendMode.OVERWRITE, true)) {

            // Cabeçalho
            float y = PAGE_HEIGHT - MARGIN;
            cs.setLineWidth(1.5f);
            cs.moveTo(MARGIN, y); cs.lineTo(PAGE_WIDTH - MARGIN, y); cs.stroke();
            y -= 18;

            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_BOLD), 13);
            cs.newLineAtOffset(MARGIN, y);
            cs.showText("HISTÓRICO DE ASSINATURAS DIGITAIS");
            cs.endText();
            y -= 14;

            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA), 8);
            cs.newLineAtOffset(MARGIN, y);
            cs.showText("Documento assinado eletronicamente conforme ICP-Brasil / PAdES (DOC-ICP-15)");
            cs.endText();
            y -= 10;

            cs.setLineWidth(0.5f);
            cs.moveTo(MARGIN, y); cs.lineTo(PAGE_WIDTH - MARGIN, y); cs.stroke();

            // Desenha todos os 10 blocos
            float blocksTop = PAGE_HEIGHT - MARGIN - HEADER_HEIGHT;
            for (int idx = 1; idx <= MAX_SIGNATURES; idx++) {
                float blockY = blocksTop - (idx - 1) * BLOCK_HEIGHT;

                if (idx == firstSignatureIndex) {
                    // Bloco preenchido com dados da primeira assinatura
                    renderSignatureBlockContent(cs, firstMetadata, idx, blockY);
                }
                // Slots futuros ficam completamente em branco — sem borda, sem label.
                // O espaço é reservado implicitamente pelo layout da página.
            }

            // Rodapé
            float footerY = MARGIN + FOOTER_HEIGHT - 8;
            cs.setLineWidth(0.5f);
            cs.moveTo(MARGIN, footerY + 14); cs.lineTo(PAGE_WIDTH - MARGIN, footerY + 14); cs.stroke();

            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_OBLIQUE), 7);
            cs.newLineAtOffset(MARGIN, footerY);
            cs.showText("Este documento possui assinatura(s) digital(is) válida(s). " +
                    "Para verificar, acesse: https://verificador.iti.gov.br");
            cs.endText();

            String geradoEm = "Gerado em: " + new SimpleDateFormat("dd/MM/yyyy HH:mm:ss").format(new Date());
            float tw;
            try { tw = new PDType1Font(Standard14Fonts.FontName.HELVETICA).getStringWidth(geradoEm) / 1000 * 7; }
            catch (IOException ex) { tw = 100f; }

            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA), 7);
            cs.newLineAtOffset(PAGE_WIDTH - MARGIN - tw, footerY);
            cs.showText(geradoEm);
            cs.endText();
        }
    }

    // =========================
    // PREENCHER BLOCO EXISTENTE (2ª+ assinatura, incremental)
    // Abre a última página e preenche o bloco correto via APPEND.
    // =========================

    private void fillSignatureBlockInPage(PDDocument doc, SignatureMetadata metadata,
                                          int signatureIndex) throws IOException {
        // A página de metadados é sempre a última
        PDPage page = doc.getPage(doc.getNumberOfPages() - 1);

        try (PDPageContentStream cs = new PDPageContentStream(
                doc, page, PDPageContentStream.AppendMode.APPEND, true)) {

            float blocksTop = PAGE_HEIGHT - MARGIN - HEADER_HEIGHT;
            float blockY = blocksTop - (signatureIndex - 1) * BLOCK_HEIGHT;

            // Preenche apenas o conteúdo do bloco (a borda já foi desenhada na criação)
            renderSignatureBlockContent(cs, metadata, signatureIndex, blockY);
        }
    }

    // =========================
    // RENDERIZAÇÃO DOS BLOCOS
    // =========================

    /**
     * Preenche o conteúdo de um bloco com os dados da assinatura.
     */
    private void renderSignatureBlockContent(PDPageContentStream cs, SignatureMetadata metadata,
                                             int index, float y) throws IOException {
        float x      = MARGIN;
        float w      = PAGE_WIDTH - MARGIN * 2;
        float h      = BLOCK_HEIGHT - 4;
        float innerX = x + 6;
        float textY  = y - 12;

        // Borda mais espessa para blocos preenchidos
        cs.setLineWidth(0.6f);
        cs.addRect(x, y - h, w, h);
        cs.stroke();

        if (metadata == null) return;

        cs.beginText();
        cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_BOLD), 8);
        cs.newLineAtOffset(innerX, textY);
        cs.showText("ASSINATURA #" + index);
        cs.endText();
        textY -= 11;

        if (metadata.getSignerName() != null) {
            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_BOLD), 7);
            cs.newLineAtOffset(innerX, textY);
            cs.showText("Assinante: ");
            cs.endText();
            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA), 7);
            cs.newLineAtOffset(innerX + 45, textY);
            cs.showText(truncate(metadata.getSignerName(), 60));
            cs.endText();
            textY -= 10;
        }

        if (metadata.getReason() != null) {
            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_BOLD), 7);
            cs.newLineAtOffset(innerX, textY);
            cs.showText("Cargo/Motivo: ");
            cs.endText();
            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA), 7);
            cs.newLineAtOffset(innerX + 56, textY);
            cs.showText(truncate(metadata.getReason(), 55));
            cs.endText();
            textY -= 10;
        }

        String dateStr = new SimpleDateFormat("dd/MM/yyyy HH:mm:ss").format(new Date());
        cs.beginText();
        cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_BOLD), 7);
        cs.newLineAtOffset(innerX, textY);
        cs.showText("Data: ");
        cs.endText();
        cs.beginText();
        cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA), 7);
        cs.newLineAtOffset(innerX + 25, textY);
        cs.showText(dateStr);
        cs.endText();

        if (metadata.getLocation() != null) {
            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_BOLD), 7);
            cs.newLineAtOffset(innerX + 130, textY);
            cs.showText("Local: ");
            cs.endText();
            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA), 7);
            cs.newLineAtOffset(innerX + 152, textY);
            cs.showText(truncate(metadata.getLocation(), 35));
            cs.endText();
        }
        textY -= 10;

        if (metadata.getIpAddress() != null) {
            cs.beginText();
            cs.setFont(new PDType1Font(Standard14Fonts.FontName.HELVETICA_OBLIQUE), 6);
            cs.newLineAtOffset(innerX, textY);
            cs.showText("IP: " + metadata.getIpAddress() +
                    (metadata.getDocumentId() != null ? "  |  Doc: " + metadata.getDocumentId() : ""));
            cs.endText();
        }
    }

    // =========================
    // INJEÇÃO DE CHAVE PÚBLICA
    // =========================

    private void injectPublicKeyIntoDoc(PDDocument doc, String publicKey,
                                        int signatureIndex) throws IOException {
        String fieldName = PUBLIC_KEY_FIELD_PREFIX + "_" + signatureIndex;

        var acroForm = doc.getDocumentCatalog().getAcroForm();
        if (acroForm == null) {
            acroForm = new org.apache.pdfbox.pdmodel.interactive.form.PDAcroForm(doc);
            doc.getDocumentCatalog().setAcroForm(acroForm);
        }

        if (acroForm.getField(fieldName) != null) {
            log("WARN: campo " + fieldName + " já existe — ignorado");
            return;
        }

        var field = new org.apache.pdfbox.pdmodel.interactive.form.PDTextField(acroForm);
        field.setPartialName(fieldName);
        field.setFieldFlags(2);
        field.getCOSObject().setString(org.apache.pdfbox.cos.COSName.V, publicKey);
        acroForm.getFields().add(field);

        log("Campo criado: " + fieldName + " = "
                + publicKey.substring(0, Math.min(20, publicKey.length())) + "...");
    }

    // =========================
    // DSS — HELPERS
    // =========================

    private PAdESService buildPAdESService() {
        CommonCertificateVerifier verifier = new CommonCertificateVerifier();
        verifier.setCheckRevocationForUntrustedChains(false);
        PAdESService service = new PAdESService(verifier);
        service.setPdfObjFactory(new PdfBoxNativeObjectFactory());
        return service;
    }

    private PAdESSignatureParameters buildSignatureParameters(SignatureMetadata metadata,
                                                              int signatureIndex) {
        PAdESSignatureParameters params = new PAdESSignatureParameters();
        params.setSignatureLevel(SignatureLevel.PAdES_BASELINE_B);
        params.setSignaturePackaging(SignaturePackaging.ENVELOPED);
        params.setDigestAlgorithm(DigestAlgorithm.SHA256);
        params.setGenerateTBSWithoutCertificate(true);

        if (metadata != null) {
            if (metadata.getSignerName() != null) params.setSignerName(metadata.getSignerName());
            if (metadata.getReason()     != null) params.setReason(metadata.getReason());
            if (metadata.getLocation()   != null) params.setLocation(metadata.getLocation());
        }

        SignatureImageParameters imageParams = new SignatureImageParameters();
        SignatureFieldParameters  fieldParams = new SignatureFieldParameters();
        fieldParams.setOriginX(0);
        fieldParams.setOriginY(0);
        fieldParams.setWidth(1);
        fieldParams.setHeight(1);
        fieldParams.setPage(1);
        imageParams.setFieldParameters(fieldParams);
        params.setImageParameters(imageParams);

        return params;
    }

    // =========================
    // EXTRAIR BYTERANGE — ÚLTIMA ASSINATURA
    // Para múltiplas assinaturas, o PDF terá vários /ByteRange.
    // Precisamos sempre do ÚLTIMO (o mais recente, com maior offset).
    // =========================

    private int[] extractLastByteRangeFromPdf(byte[] pdfBytes) {
        String marker      = "/ByteRange [";
        byte[] markerBytes = marker.getBytes(java.nio.charset.StandardCharsets.US_ASCII);

        int[] lastByteRange = null;

        for (int i = 0; i < pdfBytes.length - markerBytes.length; i++) {
            boolean found = true;
            for (int j = 0; j < markerBytes.length; j++) {
                if (pdfBytes[i + j] != markerBytes[j]) { found = false; break; }
            }
            if (!found) continue;

            int pos = i + markerBytes.length;
            int[] values = new int[4];
            int count = 0;

            while (pos < pdfBytes.length && count < 4) {
                while (pos < pdfBytes.length
                        && (pdfBytes[pos] == ' ' || pdfBytes[pos] == '\t'))
                    pos++;

                if (pos >= pdfBytes.length || pdfBytes[pos] == ']') break;

                StringBuilder num = new StringBuilder();
                while (pos < pdfBytes.length
                        && pdfBytes[pos] >= '0' && pdfBytes[pos] <= '9') {
                    num.append((char) pdfBytes[pos++]);
                }

                if (num.length() == 0) { pos++; continue; }
                values[count++] = Integer.parseInt(num.toString());
            }

            if (count == 4 && values[1] > 0 && values[3] > 0) {
                // Guarda o ByteRange com maior offset (o mais recente)
                if (lastByteRange == null || values[2] > lastByteRange[2]) {
                    lastByteRange = values;
                }
            }
        }

        if (lastByteRange != null) {
            log("Último ByteRange: [" + lastByteRange[0] + ", " + lastByteRange[1]
                    + ", " + lastByteRange[2] + ", " + lastByteRange[3] + "]");
        } else {
            log("WARN: ByteRange não encontrado nos bytes do PDF");
        }

        return lastByteRange;
    }

    // =========================
    // CACHE DE PARAMS DSS
    // =========================

    private void saveParamsCache(String preparedPath, PAdESSignatureParameters params,
                                 byte[] byteRangeContent, SignatureMetadata metadata) throws IOException {
        Files.write(Paths.get(preparedPath + ".cache"), byteRangeContent);
        Files.writeString(Paths.get(preparedPath + ".algo"), params.getSignatureAlgorithm().name());

        if (metadata != null) {
            String metaLine = nullToEmpty(metadata.getSignerName()) + "|"
                    + nullToEmpty(metadata.getReason())             + "|"
                    + nullToEmpty(metadata.getLocation());
            Files.writeString(Paths.get(preparedPath + ".meta"), metaLine);
        }
    }

    private ParamsCache loadParamsCache(String preparedPath) throws Exception {
        File cacheFile = new File(preparedPath + ".cache");
        File algoFile  = new File(preparedPath + ".algo");

        if (!cacheFile.exists() || !algoFile.exists())
            throw new ValidationException(
                    "Cache de assinatura não encontrado. Execute /preparar primeiro.");

        byte[] byteRangeContent = Files.readAllBytes(cacheFile.toPath());

        PAdESSignatureParameters params = new PAdESSignatureParameters();
        params.setSignatureLevel(SignatureLevel.PAdES_BASELINE_B);
        params.setSignaturePackaging(SignaturePackaging.ENVELOPED);
        params.setDigestAlgorithm(DigestAlgorithm.SHA256);
        params.setGenerateTBSWithoutCertificate(true);

        File metaFile = new File(preparedPath + ".meta");
        if (metaFile.exists()) {
            String[] parts = Files.readString(metaFile.toPath()).split("\\|", -1);
            if (parts.length >= 1 && !parts[0].isBlank()) params.setSignerName(parts[0]);
            if (parts.length >= 2 && !parts[1].isBlank()) params.setReason(parts[1]);
            if (parts.length >= 3 && !parts[2].isBlank()) params.setLocation(parts[2]);
        }

        SignatureImageParameters imageParams = new SignatureImageParameters();
        SignatureFieldParameters  fieldParams = new SignatureFieldParameters();
        fieldParams.setOriginX(0);
        fieldParams.setOriginY(0);
        fieldParams.setWidth(1);
        fieldParams.setHeight(1);
        fieldParams.setPage(1);
        imageParams.setFieldParameters(fieldParams);
        params.setImageParameters(imageParams);

        return new ParamsCache(params, byteRangeContent);
    }

    private void deleteParamsCache(String preparedPath) {
        for (String ext : new String[]{".cache", ".algo", ".meta"}) {
            try { Files.deleteIfExists(Paths.get(preparedPath + ext)); }
            catch (IOException ignored) {}
        }
    }

    // =========================
    // UTILS
    // =========================

    private int countExistingSignatures(File file) throws IOException {
        try (PDDocument doc = Loader.loadPDF(file)) {
            return doc.getSignatureDictionaries().size();
        }
    }

    private void validateInputPath(String inputPath) throws Exception {
        if (inputPath == null || inputPath.isBlank())
            throw new ValidationException("Caminho do arquivo não pode ser vazio");
        if (!inputPath.toLowerCase().endsWith(".pdf"))
            throw new ValidationException("Apenas arquivos PDF são suportados");
        File file = new File(inputPath);
        if (!file.exists())
            throw new FileNotFoundException("Arquivo não encontrado: " + inputPath);
        if (!file.canRead())
            throw new SecurityException("Sem permissão de leitura no arquivo");
        if (file.length() > 50L * 1024 * 1024)
            throw new ValidationException("Arquivo muito grande. Máximo: 50MB");
    }

    private void validateMetadata(SignatureMetadata metadata) throws ValidationException {
        if (metadata.getDocumentId() != null && metadata.getDocumentId().length() > 100)
            throw new ValidationException("Document ID muito longo");
        if (metadata.getSignerName() != null && metadata.getSignerName().length() > 200)
            throw new ValidationException("Nome muito longo");
    }

    private String truncate(String s, int max) {
        if (s == null) return "";
        return s.length() <= max ? s : s.substring(0, max - 3) + "...";
    }

    private String nullToEmpty(String s) { return s == null ? "" : s; }

    private void log(String msg) { System.out.println("[PdfService] " + msg); }

    // =========================
    // CLASSES INTERNAS
    // =========================

    private static class ParamsCache {
        final PAdESSignatureParameters params;
        final byte[] byteRangeContent;
        ParamsCache(PAdESSignatureParameters p, byte[] b) { params = p; byteRangeContent = b; }
    }

    public class PrepareResponse {
        private byte[]           toBeSigned;
        private String           toBeSignedBase64;
        private String           preparedFilePath;
        private String           fileName;
        private SignatureMetadata metadata;
        private int              signatureIndex;

        public PrepareResponse(byte[] toBeSigned, String toBeSignedBase64,
                               String preparedFilePath, String fileName,
                               SignatureMetadata metadata, int signatureIndex) {
            this.toBeSigned       = toBeSigned;
            this.toBeSignedBase64 = toBeSignedBase64;
            this.preparedFilePath = preparedFilePath;
            this.fileName         = fileName;
            this.metadata         = metadata;
            this.signatureIndex   = signatureIndex;
        }

        public byte[]            getToBeSigned()       { return toBeSigned; }
        public String            getToBeSignedBase64() { return toBeSignedBase64; }
        public String            getPreparedFilePath() { return preparedFilePath; }
        public String            getFileName()         { return fileName; }
        public SignatureMetadata getMetadata()         { return metadata; }
        public int               getSignatureIndex()   { return signatureIndex; }
    }

    public static class SignatureMetadata {
        private String documentId, signerName, reason, location, ipAddress, userAgent, publicKey;

        public String getDocumentId()         { return documentId; }
        public void   setDocumentId(String v) { this.documentId = v; }
        public String getSignerName()         { return signerName; }
        public void   setSignerName(String v) { this.signerName = v; }
        public String getReason()             { return reason; }
        public void   setReason(String v)     { this.reason = v; }
        public String getLocation()           { return location; }
        public void   setLocation(String v)   { this.location = v; }
        public String getIpAddress()          { return ipAddress; }
        public void   setIpAddress(String v)  { this.ipAddress = v; }
        public String getUserAgent()          { return userAgent; }
        public void   setUserAgent(String v)  { this.userAgent = v; }
        public String getPublicKey()          { return publicKey; }
        public void   setPublicKey(String v)  { this.publicKey = v; }
    }

    public static class SignatureInfo {
        private int     index, signatureSize;
        private String  name, reason, location, filter, subFilter,
                signatureBase64, byteRangeHashBase64, publicKeyBase64, toBeSignedBase64;
        private Date    signDate;
        private int[]   byteRange;
        private boolean byteRangeValid;

        public int     getIndex()                        { return index; }
        public void    setIndex(int v)                   { this.index = v; }
        public String  getName()                         { return name; }
        public void    setName(String v)                 { this.name = v; }
        public String  getReason()                       { return reason; }
        public void    setReason(String v)               { this.reason = v; }
        public String  getLocation()                     { return location; }
        public void    setLocation(String v)             { this.location = v; }
        public Date    getSignDate()                     { return signDate; }
        public void    setSignDate(Date v)               { this.signDate = v; }
        public String  getFilter()                       { return filter; }
        public void    setFilter(String v)               { this.filter = v; }
        public String  getSubFilter()                    { return subFilter; }
        public void    setSubFilter(String v)            { this.subFilter = v; }
        public String  getSignatureBase64()              { return signatureBase64; }
        public void    setSignatureBase64(String v)      { this.signatureBase64 = v; }
        public int     getSignatureSize()                { return signatureSize; }
        public void    setSignatureSize(int v)           { this.signatureSize = v; }
        public int[]   getByteRange()                    { return byteRange; }
        public void    setByteRange(int[] v)             { this.byteRange = v; }
        public boolean isByteRangeValid()                { return byteRangeValid; }
        public void    setByteRangeValid(boolean v)      { this.byteRangeValid = v; }
        public String  getByteRangeHashBase64()          { return byteRangeHashBase64; }
        public void    setByteRangeHashBase64(String v)  { this.byteRangeHashBase64 = v; }
        public String  getPublicKeyBase64()              { return publicKeyBase64; }
        public void    setPublicKeyBase64(String v)      { this.publicKeyBase64 = v; }
        public String  getToBeSignedBase64()             { return toBeSignedBase64; }
        public void    setToBeSignedBase64(String v)     { this.toBeSignedBase64 = v; }
    }

    public static class ValidationException extends Exception {
        public ValidationException(String message) { super(message); }
    }
}