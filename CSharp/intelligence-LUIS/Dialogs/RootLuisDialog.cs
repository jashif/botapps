//#define useSampleModel
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
namespace LuisBot.Dialogs
{

    [LuisModel("b7ee19ae-b6b5-47c1-8a52-f567bbd6be29", "d9228b45e7d54b2c98dd173f002f83c4")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        [LuisIntent("GetExpense")]
        public async Task GetExpense(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;

            await context.PostAsync($"Welcome to the expense buddy support! we are analyzing your message: '{message.Text}'...");
            var hotelsQuery = new ExpenseQuery();
            var hotelsFormDialog = new FormDialog<ExpenseQuery>(hotelsQuery, this.BuildHotelsForm, FormOptions.PromptInStart, result.Entities);
            context.Call(hotelsFormDialog, this.ResumeAfterHotelsFormDialog);
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";
            await context.PostAsync(message);
            context.Wait(this.MessageReceived);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hi! please type like 'find my expense','get my expense' etc");
            context.Wait(this.MessageReceived);
        }
        private async Task ResumeAfterHotelsFormDialog(IDialogContext context, IAwaitable<ExpenseQuery> result)
        {
            try
            {
                var searchQuery = await result;
                var response = await this.GetExpenseByEmailId(searchQuery.EmailId);
                var resultMessage = context.MakeMessage();
                if (response.status)
                {
                    await context.PostAsync("I found your expense");
                    resultMessage.Text = "your expense for current month  is Rs: " + response.amount;
                    await context.PostAsync(resultMessage);
                }
                else
                {
                    await context.PostAsync("Sorry, we couldn't get your details. may be your are not registered in our system");
                }
            }

            catch (FormCanceledException ex)
            {
                string reply;
                if (ex.InnerException == null)
                {

                    reply = "You have canceled the operation.";
                }
                else
                {

                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";

                }
                await context.PostAsync(reply);

            }
            finally
            {

                context.Done<object>(null);

            }
        }

        public async Task<ExpenseData> GetExpenseByEmailId(string userid)
        {

            HttpClient client = new HttpClient();
            // client.BaseAddress = new Uri("http://expensebuddy.azurewebsites.net/api/botdata/myexpense?emailId=" + userid);

            client.DefaultRequestHeaders.Accept.Add(

                new MediaTypeWithQualityHeaderValue("application/json"));

            // string parameter = string.Format(Consts.LookupBikesWithUserAPI, userid);

            HttpResponseMessage response = client.GetAsync("http://expensebuddy.azurewebsites.net/api/botdata/myexpense?emailId=" + userid).Result;

            if (response.IsSuccessStatusCode)

            {

                JsonSerializerSettings settings = new JsonSerializerSettings();

                String responseString = await response.Content.ReadAsStringAsync();

                var responseElement = JsonConvert.DeserializeObject<ExpenseData>(responseString, settings);

                return responseElement;

            }
            else
            {

                return null;

            }
        }

        private IForm<ExpenseQuery> BuildHotelsForm()
        {

            OnCompletionAsyncDelegate<ExpenseQuery> processHotelsSearch = async (context, state) =>

            {

                var message = "Getting  expense of ";

                if (!string.IsNullOrEmpty(state.EmailId))

                {

                    message += $"in {state.EmailId}...";

                }

                await context.PostAsync(message);

            };



            return new FormBuilder<ExpenseQuery>()

                .Field(nameof(ExpenseQuery.EmailId), (state) => string.IsNullOrEmpty(state.EmailId))
                .OnCompletion(processHotelsSearch)
                .Build();

        }
    }

    [Serializable]
    public class ExpenseQuery
    {

        [Prompt("Please enter your {&}")]

        [Optional]
        public string EmailId { get; set; }

    }

    [Serializable]
    public class ExpenseData
    {

        public double amount { get; set; }

        public bool status { get; set; }

        //{"amount":2908.0,"status":true}

    }
}
