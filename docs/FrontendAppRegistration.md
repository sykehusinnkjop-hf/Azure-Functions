# Frontend App Registration

Registrering av en applikasjon i AAD som har tilgang til og lese/skrive brukerkonto atributter.

* Navn: **Leder-Appen**
* Supported account types: **Single Tenant**
![Navigate to deployment center](/docs/img/lederappen-steg1.png)


## Authentication
* https://login.microsoftonline.com/common/oauth2/nativeclient
![Navigate to deployment center](/docs/img/Lederappen-Steg2.png)

## API Permissions
* **Delegated** Leder-API -> *leder*
* **Delegated** Microsoft Graph -> *User.Read*

**Grant consent må gjennomføres av en administrator**
![Navigate to deployment center](/docs/img/Lederappen-Steg3.png)  

## Registrer Lederappen som en kjent client hos Leder API

1. kopier Application ID til lederappen
![Navigate to deployment center](/docs/img/Lederappen-Steg4.png)

2. Naviger til **Leder API -> Manifest**, Legg til Lederappen som en *KnownClientApplications*.
![Navigate to deployment center](/docs/img/Lederappen-Steg5.png)