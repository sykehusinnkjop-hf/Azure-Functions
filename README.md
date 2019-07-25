# Bruker-Kontroll-API (Give me a name)
Bruker Kontroll is an app made to sit between your front-end application and the microsoft Graph API. "Bruker kontroll" allows you to request information about users based on your "role" in the organization.

## intended usage

# Getting Started
All stages must be completed in order to deploy the function app.
- How to Deploy
- AD Authentication
- Register the a

## How To Deploy



Log in to your Azure Portal And select **App Services > +Add**.

0. Make sure to select the ***Windows*** OS and the ***.Net Core*** runtime.                                                    
![Create a Function](/docs/img/createFunctionApp.PNG)



1. In your function app in the Azure portal, select **Platform features** > **Deployment Center**
![Navigate to deployment center](/docs/img/navigateDeployment.jpg)

2. In the **Deployment Center**, select **GitHub**, and then select **Authorize**. Or, if you've already authorized GitHub, select **Continue**.
![Navigate to deployment center](/docs/img/selectGithub.png)

3. In GitHub, Select **Authorize AzureAppService**.
![Navigate to deployment center](/docs/img/authorize.png)
In the Azure portal Deployment Center, select Continue.

4. Select **App Service build service** 
![Navigate to deployment center](/docs/img/build.png)

5.  
    - organization should be **sykehusinnkjop-hf**. 
    - repository should be **Bruker-Kontroll-AZ-func**. 
    - Branch should be **Master** for the production ready release.

![Navigate to deployment center](/docs/img/selectRepository.png)

6. Finally, review all details and select Finish to complete your deployment configuration.
![Navigate to deployment center](/docs/img/summary.png)             
You have now deployed the code into the Azure Function enviroment. Next up we need to configure it.




## Register the function with AD

We need to register the application as a client for azure AD, and give it the right permissions for

1. Select **Azure Active Directory > App Registrations > +New registrations**
![Navigate to deployment center](/docs/img/registerApplication.png)                                                                                             

2. Register a name that is easily identifiable, then select **Accounts in this organizational directory only**. No need for a Redirect URL since the app is using "client credential" authentication.
![Navigate to deployment center](/docs/img/registerApplicationName.png)                       
*users should and will not log in with this registration, it would give them indiidually elivated priviliges to the graph, We will later register a separate service for authentication.*

3. Take a note of the **tenant ID** and the **Application ID** as these will be used when configuring the application later.                                            
![Navigate to deployment center](/docs/img/registeredIDs.png)  

4. Generate a secret by going to **Certificates & Secrets > +New client secret**. Write a short description so people in the future will know what the secret is for.
![Navigate to deployment center](/docs/img/generateSecret.png)  

    - Make sure to write down the secret, as it will be unavailable in 15 minutes

5. Adding permissions.
    - select **API permissions > +Add a permission**
    ![Navigate to deployment center](/docs/img/APIPermissions_1.png)

    - select **Microsoft Graph**
    ![Navigate to deployment center](/docs/img/APIPermissions_2.png) 

    - select **Application permissions**

        ![Navigate to deployment center](/docs/img/APIPermissions_3.png) 

    - Add the following permissions:
        - Directory.ReadWrite.All
        - Group.Read.All 
        - User.ReadWrite.All


6. Get an Administrator to grant consent for the API permissions.
![Navigate to deployment center](/docs/img/APIPermissions_4.png) 


## Setting up Security group

1. Create an **Azure AD security group** or use an existing security group containing your managers. Only users assigned to this group will be allowed to make requests to this API.
**Take note of the ObjectID for the security group.**
![Navigate to deployment center](/docs/img/securityGroup.png) 


## Add enviroment variables
1. Navigate back to your function app and select **Configuration**
![Navigate to deployment center](/docs/img/selectConfiguration.png) 

2. To add a setting select **+ New application setting**. Add the following settings
    - tenant_ID 
    - security_group_ID
    - application_ID
    - application_secret
    - force_change_password_new_users   *OPTIONAL* **true** or **false**: Defaults to false if not set
    - force_mfa_on_new_users            *OPTIONAL* **true** or **false**: Defaults to false if not set

    Select **Save** once you are finnished
    ![Navigate to deployment center](/docs/img/applicationSettings.png) 

## Authentication/Authorization

Authentication should be configured through Azure API management. The function **WILL NOT validate token information**, so tokens need to be pre validated before the function is called. 

The function should be secured via Certificates or Function Secrets to the API Management layer.