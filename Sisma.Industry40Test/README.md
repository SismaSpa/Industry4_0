Esempio di client MQTT per le macchine Sisma
=================

Introduzione
-----
Il progetto **Sisma.Industry40Test** è un esempio di come connettersi alle macchine Sisma attraverso il protocollo MQTT, in modo da ricevere o inviare dati.    
L'esempio è sviluppato nel linguaggio C# e consiste di una console application dove è possibile:
* connettersi e disconnettersi ad un broker MQTT;
* leggere il messaggio di stato della macchina;
* inviare un messaggio di informazioni alla macchina;

Librerie usate
----
Nell'esempio vengono referenziate le seguenti librerie:
* **MQTTnet**: utilizzata per la creazione del client MQTT;
* **Newtonsoft.JSON**: utilizzata per serializzare/deserializzare i messaggi in formato JSON della comunicazione;

Metodi
----
Fare riferimento al file **apiDocs.html** per la documentazione tecnica dei metodi dell'esempio.
