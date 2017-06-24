/*
 * MIT License
 *
 * Copyright (c) 2017 Katherine Marino
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace CatFacts {
    public class Function {

        //--- Class Fields ---
        private static readonly string[] RANDOM_FACT_INTROS = { 
            "Here is your cat fact.",
            "Did you know?",
            "I thought this was interesting.",
            "Here's something cool.",
            "Here's a good one."
        };

        //--- Fields ---
        private readonly AmazonDynamoDBClient _dynamoClient;
        private readonly Random _random;
        private readonly string _tableName;
        private long _factCount;

        //--- Constructors ---
        public Function() {

            // read function settings
            _tableName = System.Environment.GetEnvironmentVariable("catfacts_db");
            if(string.IsNullOrEmpty(_tableName)) {
                throw new ArgumentException("missing table name for cat facts dynamodb table", "catfacts_db");
            }

            // initialize variables
            _dynamoClient = new AmazonDynamoDBClient();
            _random = new Random();

            // read number of facts in DB
            var request = _dynamoClient.DescribeTableAsync(_tableName);
            request.Wait();
            _factCount = request.Result.Table.ItemCount;
            LambdaLogger.Log($"*** INFO: Found {_factCount} items in dynamo table {_tableName}.");
        }

        //--- Methods ---
        public SkillResponse FunctionHandler(SkillRequest skill, ILambdaContext context) {

            // decode skill request
            IEnumerable<AFactResponse> responses;
            IEnumerable<AFactResponse> reprompt = null;
            switch(skill.Request) {

                // skill was activated without an intent
                case LaunchRequest launch:
                    LambdaLogger.Log($"*** INFO: launch\n");
                    responses = new[] { new FactResponseSay("Welcome to Cat Facts!") };
                    reprompt = new[] { new FactResponseHelp() };
                    return ResponseBuilder.Ask(
                        ConvertToSpeech(responses),
                        new Reprompt {
                            OutputSpeech = ConvertToSpeech(reprompt)
                        }
                    );

                // skill was activated with an intent
                case IntentRequest intent:

                    // check if the intent is a fact request intent
                    if(Enum.TryParse(intent.Intent.Name, true, out FactCommandType command)) {
                        LambdaLogger.Log($"*** INFO: fact request intent ({intent.Intent.Name})\n");
                        switch(command) {
                            case FactCommandType.GetFact:
                                responses = GetFactResponse();
                                break;
                            default:
                                // should never happen
                                responses = new AFactResponse[] {};
                                break;
                        }
                        reprompt = new[] { new FactResponseHelp() };
                    } else {
                        switch(intent.Intent.Name) {

                            // built-in intents
                            case BuiltInIntent.Help:
                                LambdaLogger.Log($"*** INFO: built-in help intent ({intent.Intent.Name})\n");
                                responses = new[] { new FactResponseHelp() };
                                break;

                            case BuiltInIntent.Stop:
                            case BuiltInIntent.Cancel:
                                LambdaLogger.Log($"*** INFO: built-in stop/cancel intent ({intent.Intent.Name})\n");
                                responses = new[] { new FactResponseBye() };
                                break;

                            // unknown & unsupported intents
                            default:
                                LambdaLogger.Log("*** WARNING: intent not recognized\n");
                                responses = new[] { new FactResponseHelp() };
                                reprompt = new[] { new FactResponseNotUnderstood() };
                                break;
                        }
                    }

                    // respond
                    if(reprompt != null) {
                        return ResponseBuilder.Ask(
                            ConvertToSpeech(responses),
                            new Reprompt {
                                OutputSpeech = ConvertToSpeech(reprompt)
                            }
                        );
                    }
                    return ResponseBuilder.Tell(ConvertToSpeech(responses));

                // skill session ended (no response expected)
                case SessionEndedRequest ended:
                    LambdaLogger.Log("*** INFO: session ended\n");
                    return ResponseBuilder.Empty();

                // exception reported on previous response (no response expected)
                case SystemExceptionRequest error:
                    LambdaLogger.Log("*** INFO: system exception\n");
                    LambdaLogger.Log($"*** EXCEPTION: skill request: {JsonConvert.SerializeObject(skill)}\n");
                    return ResponseBuilder.Empty();

                // unknown skill received (no response expected)
                default:
                    LambdaLogger.Log($"*** WARNING: unrecognized skill request: {JsonConvert.SerializeObject(skill)}\n");
                    return ResponseBuilder.Empty();
            }
        }

        private IOutputSpeech ConvertToSpeech(IEnumerable<AFactResponse> responses) {
            var ssml = new XElement("speak");
            foreach(var response in responses) {
                switch(response) {
                    case FactResponseSay say:
                        ssml.Add(new XElement("p", new XText(say.Text)));
                        break;
                    case FactResponseDelay delay:
                        ssml.Add(new XElement("break", new XAttribute("time", (int)delay.Delay.TotalMilliseconds + "ms")));
                        break;
                    case FactResponseNotUnderstood _:
                        ssml.Add(new XElement("p", new XText("Sorry, I don't know what that means.")));
                        break;
                    case FactResponseHelp _:
                        ssml.Add(new XElement("p", new XText("To hear a new fact, say give me a cat fact.")));
                        break;
                    case FactResponseBye _:
                        ssml.Add(new XElement("p", new XText("Good bye.")));
                        break;
                    case null:
                        LambdaLogger.Log($"ERROR: null response\n");
                        ssml.Add(new XElement("p", new XText("Sorry, I don't know what that means.")));
                        break;
                    default:
                        LambdaLogger.Log($"ERROR: unknown response: {response.GetType().Name}\n");
                        ssml.Add(new XElement("p", new XText("Sorry, I don't know what that means.")));
                        break;
                    }
            }
            return new SsmlOutputSpeech {
                Ssml = ssml.ToString(SaveOptions.DisableFormatting)
            };
        }

        private IEnumerable<AFactResponse> GetFactResponse() {
            var responses = new List<AFactResponse>();
            var id = _random.Next((int) _factCount) + 1;
            if(_random.Next(2) < 1) {
                var randomIndex = _random.Next(RANDOM_FACT_INTROS.Length + 1);
                if(randomIndex >= RANDOM_FACT_INTROS.Length) {
                    responses.Add(new FactResponseSay($"Here is cat fact number {id}"));
                } else {
                    responses.Add(new FactResponseSay(RANDOM_FACT_INTROS[randomIndex]));
                }
            }
            responses.Add(new FactResponseSay(GetFact(id)));
            return responses;
        }

        private string GetFact(int factId) {
            var request = new GetItemRequest {
                TableName = _tableName,
                Key = new Dictionary<string, AttributeValue>() {{
                    "Id", new AttributeValue() { N = factId.ToString() }
                }}
            };
            var task = _dynamoClient.GetItemAsync(request);
            task.Wait();
            AttributeValue fact;
            if (task.Result.Item.TryGetValue("Fact", out fact)) {
                LambdaLogger.Log($"INFO: Fact returned from database was: {fact.S}");
                return fact.S;
            } else {
                // GetFact();
                return "Sorry, could not retrieve a fact";
            }
        }
    }
}
