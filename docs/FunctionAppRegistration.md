# Function App Registrering

Registrering av en applikasjon i AAD som har tilgang til og lese/skrive brukerkonto atributter.

* Navn: **Leder-API**
* Supported account types: **Single Tenant**
![Navigate to deployment center](/docs/img/Leder-API-Steg1.png)     


## Certificates & Secrets
* Description: Leder API Azure function 
* Expires: Never
**Vi trenger Secret tilbake for og ta i bruk applikasjonen**
![Navigate to deployment center](/docs/img/generateSecret.png)  

## API Permissions
* **Delegated** Group.Read.All
* **Delegated** User.ReadWrite.All

**Grant consent må gjennomføres av en administrator**
![Navigate to deployment center](/docs/img/Leder-API-Steg3.png)  


## Expose an API
Må eksponere applikasjonen til en annen registrert applikasjon som blir front end applikasjonen.
![Navigate to deployment center](/docs/img/Leder-API-Steg4.png)


