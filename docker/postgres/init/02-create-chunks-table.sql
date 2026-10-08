CREATE TABLE IF NOT EXISTS chunks (
    id BIGSERIAL PRIMARY KEY,
    id_agente VARCHAR(100) NOT NULL,
    codigo_documento VARCHAR(100) NOT NULL,
    nome_documento VARCHAR(255) NOT NULL,
    incluido_em TIMESTAMPTZ NOT NULL DEFAULT now(),
    data_validade DATE,
    usuario VARCHAR(100) NOT NULL,
    texto TEXT NOT NULL,
    embedding vector(1536) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_chunks_codigo_documento
    ON chunks (codigo_documento);

CREATE INDEX IF NOT EXISTS idx_chunks_embedding_cosine
    ON chunks USING hnsw (embedding vector_cosine_ops);
