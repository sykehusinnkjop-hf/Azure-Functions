# Bruker-Kontroll-API (Give me a name)
Bruker Kontroll is an app made to sit between your front-end application and the microsoft Graph API. "Bruker kontroll" allows you to request information about users based on your "role" in the organization.

## intended usage


# How to Deploy



* Log in to your Azure Portal And Navigate to the **App Services** screen. Press the **+Add** button in the upper-left corner. 

* Make sure to select the ***Windows*** OS and the ***.Net Core*** runtime.

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
* organization should be **sykehusinnkjop-hf**. 
* repository should be **Bruker-Kontroll-AZ-func**. 
* Branch should be **Master** for the production ready release.

(If no information is showing up, please ask to get added to the organization or alternatively fork the repository to your own organization.)
![Navigate to deployment center](/docs/img/selectRepository.png)

6. Finally, review all details and select Finish to complete your deployment configuration.
![Navigate to deployment center](/docs/img/summary.png)

When the process completes, all code from the specified source is deployed to your app. At that point, changes in the deployment source trigger a deployment of those changes to your function app in Azure.






