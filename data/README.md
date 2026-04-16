# Local Data

Questa cartella documenta la struttura dei dati locali di sviluppo.

Il contenuto reale di `data/dev/` e' ignorato da Git per evitare di pubblicare dati personali.

```text
data/
  dev/
    collectify-data.json          # documento JSON locale
    collectify-data.json.bak      # backup temporaneo durante replace atomico
    collectify-data.json.*.tmp    # file temporanei di scrittura
    collectify-data.json.corrupt-*# copie preservate in caso di JSON corrotto
    assets/
      images/                     # immagini locali degli item
```

In produzione il percorso predefinito e' la cartella applicativa locale dell'utente.
