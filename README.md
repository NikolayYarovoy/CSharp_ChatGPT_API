# CSharp_ChatGPT_API
Class library for interacting with the OpenAI API

# What works 

## ChatGptClient
Provides access to a dialogue with OpenAI (like normal interaction on chat.openai.com)
### How to work with it
First you need to create an instance of the ChatGptClient class. In the simplest case it looks like this:

    ChatGptClient client = await ChatGptClient.CreateAsync("Your Token here", "gpt-3.5-turbo");

Next, you need to set the initial system and user messages:

    client.AddSystemMessage("You are a translator from English to French");
    client.AddUserMessage("Translate the text: \"Today was March 15\"");
  
And finally, you need to send a request to the API, receiving answers in the list with answers:

    List<Message> messages = await client.Request();
    
To continue the dialogue, you need to select the desired answer (by default it is the only one, but you still need to select it) and add a new custom answer:

    client.MessageSelection(0);
    client.AddUserMessage("Translate the text: \"It was raining outside\"");
    
### What else
For start, you can configure almost all parameters for the API (more details at the link: https://platform.openai.com/docs/api-reference/chat/create). To do this, you must specify the appropriate parameter in ChatGptClient.CreateAsync()

Secondly, to create a new dialog, it is not necessary to create a new instance of the class. It is enough to simply clear the message history using the ClearMessages() method:

    client.ClearMessages();
  

In case the wrong message was added by mistake, or you don't like any of the chatGPT responses, you can delete the last message using the DeleteLastMessage() method:

    client.ClearMessages();
    

You can get the entire history of messages in a dialog using the GetMessageHistory() method:

    List<Message> messages = await client.GetMessageHistory();
    
### TODO:

At the moment, the response is only received by a single message, while the API also offers streaming reading (as on chat.openai.com). It will be necessary to add such an option.

## Other modes
I am currently working on adding a ChatGPT response to the only message that has been implemented in their API.
