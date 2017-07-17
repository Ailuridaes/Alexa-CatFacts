# Alexa-CatFacts
Alexa skill to hear random facts about cats, built with the [Alexa.Net](https://github.com/timheuer/alexa-skills-dotnet) library.

The Alexa skill is driven by a lambda function (in /src/CatFacts). Facts are read from a DynamoDB table, the name of which is provided as environment variable "catfacts_db" to the lambda function.

Items can be batch loaded into the DynamoDB table via the AWS CLI using the following command:
```
aws dynamodb batch-write-item --request-items file://assets/cat-facts.json
```
Batch writes are limited to 25 table rows. Updates must be made using the update-item action instead. For more information, see the [AWS CLI documentation](http://docs.aws.amazon.com/cli/latest/reference/dynamodb/batch-write-item.html).
