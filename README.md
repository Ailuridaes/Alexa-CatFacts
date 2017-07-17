# Alexa-CatFacts
This is an Alexa skill to hear random facts about cats, built with the [Alexa.Net](https://github.com/timheuer/alexa-skills-dotnet) library.

The Alexa skill is driven by a lambda function and reads facts from a DynamoDB table.

Much of the code in this project is adapted from the [June λ# hackathon challenge to create an Alexa AdventureBot skill](https://github.com/LambdaSharp/June2017-AdventureBot).

## Setup
### Pre-requisites
Make sure the following tools are installed on your computer.

* [Install .NET Core 1.x](https://www.microsoft.com/net/core)
* [Install AWS CLI](https://aws.amazon.com/cli/)

In addition, you will need both an Amazon Developer account and an AWS account.

## Deploying the lambda function
**NOTE**: The AWS Lambda function for Alexa Skills must be deployed in `us-east-1`

### Set up an `alexa` profile
This project uses a profile named `alexa`. Use the following steps to set up this profile, or modify the `aws-lambda-tools-defaults.json` file to use an existing profile.
1. Create an `alexa` profile: `aws configure --profile alexa`
2. Configure the profile with the AWS credentials you want to use

### Create a role for the lambda function
Create an `Alexa-CatFacts` role from the AWS console. The role will need to have access to DynamoDB and CloudWatchLogs.

### Set up the DynamoDB table
1. In the AWS developer console, create a DynamoDB table to hold your facts.
2. Add facts to your table.

Items can be batch loaded into the DynamoDB table from the a specifically-formatted JSON file via the AWS CLI using the following command:
```
aws dynamodb batch-write-item --request-items file://assets/cat-facts.json
```
Batch writes are limited to 25 table rows. Updates must be made using the update-item action instead. For more information, see the [AWS CLI documentation](http://docs.aws.amazon.com/cli/latest/reference/dynamodb/batch-write-item.html).

### Publish the lambda function
1. Navigate to the Lambda function directory: `cd src/CatFacts`
2. Restore solution packages: `dotnet restore` 
3. Publish the lambda function: `dotnet lambda deploy-function`
4. Navigate to the published lambda function. 
5. Under `Code` > `Environment Variables`
    1. Add key: `catfacts_db`
    2. Add the DynamoDB table name as the value
    3. Click 'Save'.
6. Under `Triggers`
    1. Click `Add Trigger`
    2. Select `Alexa Skills Kit`
    3. Click `Submit` (**NOTE**: if `Submit` is grayed out, select `Alexa Smart Home` trigger instead and then select `Alexa Skills Kit` trigger again)

## Setting up the Alexa skill
1. [Log into the Amazon Developer Console](https://developer.amazon.com/home.html)
2. Click on the `ALEXA` tab
3. Click on Alexa Skill Kit `Get Started`
4. Click `Add a New Skill`
5. *Skill Information*
    1. Under name put: `CatFacts`
    2. Under invocation name put: `Cat Facts`
    3. Click `Save`
    4. Click `Next`
6. *Interaction Model*
    1. Click `Launch Skill Builder`
    2. Click `Discard` to proceed
    3. Click `</> Code` in left navigation
    4. Upload `assets/alexa-skill.json` file
    5. Click `Apply Changes`
    6. Click `Build Mode` in the toolbar
    7. Click `Configuration`
7. *Configuration*
    1. Select `AWS lambda ARN (Amazon Resource Name)`
    2. Select `North America`
    3. Copy the AWS lambda function ARN from its page in the developer console, and paste it in the Alexa skill configuration: `arn:aws:lambda:us-east-1:******:function:Alexa-CatFacts`
    4. Click `Next`
8. Your Alexa Skill is now available on all your registered Alexa-devices, including the Amazon mobile app.
    * For Alexa devices, say: `Alexa, open Cat Facts`
    * For the Amazon mobile app, click the microphone icon, and say: `open Cat Facts`
    * Then say, `Give me a cat fact`, or any other custom and built-in intents.
    * Say `quit` to exit the skill.