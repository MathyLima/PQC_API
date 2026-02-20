#!/bin/bash

# -------------------------------
# DIRETÓRIOS
# -------------------------------
BASE_DIR="/c/Users/mathe/Documents/PQC_API/PQC.INFRAESTRUCTURE/PostQuantumSigner/Service/Native/win-x64"
PDF_DIR="$BASE_DIR/pdfs"
KEY_DIR="$BASE_DIR/keys"
OUT="$BASE_DIR/benchmark_dsa.csv"

ALGOS=("ML-DSA-44" "ML-DSA-65" "ML-DSA-87")
NORMAL_MAX=200
SPECIAL_SIZES=(500 1024)

mkdir -p "$PDF_DIR" "$KEY_DIR"

# -------------------------------
# CABEÇALHO CSV
# -------------------------------
echo "pdf_size_mb,algo,tempo_local_ms,ciclos_local,tempo_intel_x9_ms,tempo_amd_e6_ms,custo_intel_r$,custo_amd_r$" > "$OUT"

# -------------------------------
# CLOCKS (Hz)
# -------------------------------
CPU_CLOCK_HZ_LOCAL=2600000000      # i7-13650HX
CPU_CLOCK_HZ_INTEL_X9=3000000000   # Xeon 6354
CPU_CLOCK_HZ_AMD_E6=2700000000     # EPYC 9J45

# -------------------------------
# FATORES (×1000)
# -------------------------------
FACTOR_INTEL_X9=1600   # 1.6
FACTOR_AMD_E6=1300     # 1.3
FACTOR_SCALE=1000

# -------------------------------
# CUSTOS POR HORA (R$)
# -------------------------------
PRICE_INTEL_X9=0.220448
PRICE_AMD_E6=0.165336

# -------------------------------
# GERA CHAVES
# -------------------------------
for algo in "${ALGOS[@]}"; do
  if [ ! -f "$KEY_DIR/$algo.key" ]; then
    echo "Gerando chave $algo"
    ./pqc-cli.exe keygen "$algo" "$KEY_DIR/$algo"
  fi
done

# -------------------------------
# FUNÇÃO DE ASSINATURA E BENCHMARK
# -------------------------------
sign_and_measure () {
  local size_mb=$1
  local pdf="$PDF_DIR/tmp_${size_mb}MB.pdf"

  echo "Gerando PDF ${size_mb}MB"
  dd if=/dev/zero of="$pdf" bs=1M count="$size_mb" status=none

  for algo in "${ALGOS[@]}"; do
    key="$KEY_DIR/$algo.priv"
    sig="$BASE_DIR/sig_${algo}_${size_mb}MB.bin"

    echo "Assinando ${size_mb}MB com $algo"

    start=$(date +%s%N)
    ./pqc-cli.exe sign "$pdf" "$key" "$sig"
    end=$(date +%s%N)

    elapsed_ns=$((end - start))
    elapsed_ms=$(( elapsed_ns / 1000000 ))

    # -------------------------------
    # CICLOS LOCAIS
    # -------------------------------
    cycles_local=$(( elapsed_ns * CPU_CLOCK_HZ_LOCAL / 1000000000 ))

    # -------------------------------
    # TEMPO ESTIMADO E CUSTO (Python)
    # -------------------------------
    read intel_ms amd_ms custo_intel custo_amd <<< $(python - <<END
elapsed_ns = $elapsed_ns
CPU_CLOCK_HZ_LOCAL = $CPU_CLOCK_HZ_LOCAL
CPU_CLOCK_HZ_INTEL_X9 = $CPU_CLOCK_HZ_INTEL_X9
CPU_CLOCK_HZ_AMD_E6 = $CPU_CLOCK_HZ_AMD_E6
FACTOR_INTEL_X9 = $FACTOR_INTEL_X9
FACTOR_AMD_E6 = $FACTOR_AMD_E6
FACTOR_SCALE = $FACTOR_SCALE
PRICE_INTEL_X9 = $PRICE_INTEL_X9
PRICE_AMD_E6 = $PRICE_AMD_E6

# Tempo estimado (ms)
intel_ms = round(elapsed_ns * CPU_CLOCK_HZ_LOCAL * FACTOR_INTEL_X9 / (CPU_CLOCK_HZ_INTEL_X9 * FACTOR_SCALE * 1_000_000), 2)
amd_ms = round(elapsed_ns * CPU_CLOCK_HZ_LOCAL * FACTOR_AMD_E6 / (CPU_CLOCK_HZ_AMD_E6 * FACTOR_SCALE * 1_000_000), 2)

# Custo em R$
custo_intel = round(intel_ms / 1000 / 3600 * PRICE_INTEL_X9 * 1000, 5)
custo_amd   = round(amd_ms   / 1000 / 3600 * PRICE_AMD_E6 * 1000, 5)

print(f"{intel_ms} {amd_ms} {custo_intel} {custo_amd}")
END
)

    # -------------------------------
    # GRAVA NO CSV
    # -------------------------------
    echo "$size_mb,$algo,$elapsed_ms,$cycles_local,$intel_ms,$amd_ms,$custo_intel,$custo_amd" >> "$OUT"

    rm -f "$sig"
  done

  rm -f "$pdf"
}

# -------------------------------
# BENCHMARK
# -------------------------------
for size_mb in $(seq 1 $NORMAL_MAX); do
  sign_and_measure "$size_mb"
done

for size_mb in "${SPECIAL_SIZES[@]}"; do
  sign_and_measure "$size_mb"
done

echo "Benchmark finalizado. CSV gerado em $OUT"
