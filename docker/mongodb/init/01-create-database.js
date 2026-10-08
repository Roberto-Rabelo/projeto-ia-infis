db = db.getSiblingDB('bd-historico-atendimentos');
db.createCollection('conversas');
db.conversas.createIndex({ usuario: 1, dataHora: -1 });
