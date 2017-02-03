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
using LuisBot.Model;

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
            string userName;
            if (context.UserData.TryGetValue(ContextConstants.EmailKey, out userName))
            {

            }
            //await context.PostAsync($"Welcome to the expense buddy support! we are analyzing your message: '{message.Text}'...");
            var expensequery = new ExpenseQuery();
            if (!string.IsNullOrEmpty(userName))
            {
                expensequery.EmailId = userName;
            }
            var hotelsFormDialog = new FormDialog<ExpenseQuery>(expensequery, this.BuildExpenseForm, FormOptions.PromptInStart, result.Entities);
            context.Call(hotelsFormDialog, this.ResumeAfterExpenseFormDialog);

        }

        [LuisIntent("AddExpense")]
        public async Task AddExpense(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            string userName;
            if (context.UserData.TryGetValue(ContextConstants.EmailKey, out userName))
            {

            }
            //await context.PostAsync($"Welcome to the expense buddy support! we are analyzing your message: '{message.Text}'...");
            var addexpensequery = new AddExpenseQuery();
            if (!string.IsNullOrEmpty(userName))
            {
                addexpensequery.EmailId = userName;
            }
            // await context.PostAsync($"Welcome to the expense buddy support! we are analyzing your message: '{message.Text}'...");

            var addexpenseFormDialog = new FormDialog<AddExpenseQuery>(addexpensequery, this.AddExpenseForm, FormOptions.PromptInStart, result.Entities);
            context.Call(addexpenseFormDialog, this.ResumeAfterAddexpenseFormDialog);
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {
            await context.PostAsync($"Welcome to the expense buddy support!");

            string userName;
            if (!context.UserData.TryGetValue(ContextConstants.EmailKey, out userName))
            {
                PromptDialog.Text(context, this.ResumeAfterPrompt, "Before get started, please tell me your registered emailid?");
                return;
            }
            else
            {
                string message = $"{userName} Sorry, I did not understand'. Type 'help' if you need assistance.";
                await context.PostAsync(message);
                context.Wait(this.MessageReceived);
            }


        }
        private async Task ResumeAfterPrompt(IDialogContext context, IAwaitable<string> result)
        {
            try
            {
                var userName = await result;

                // this.userWelcomed = true;

                await context.PostAsync($"Welcome {userName}!, Type 'help' if you need assistance.");

                context.UserData.SetValue(ContextConstants.EmailKey, userName);
                //string message = $"Sorry, I did not understand '{result.Query}'. Type 'help' if you need assistance.";
                // await context.PostAsync(message);
            }
            catch (TooManyAttemptsException)
            {
            }

            context.Done<object>(null);
        }

        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            await context.PostAsync("Hi! please find the options \n 1) 'type 'expense' or 'find my expense', 'get my expense' \n 2) Add expense or Add Expense for me , Please add expense");
            context.Wait(this.MessageReceived);
        }
        private async Task ResumeAfterExpenseFormDialog(IDialogContext context, IAwaitable<ExpenseQuery> result)
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

        private async Task ResumeAfterAddexpenseFormDialog(IDialogContext context, IAwaitable<AddExpenseQuery> result)
        {
            try
            {
                var searchQuery = await result;
                string userName;
                if (context.UserData.TryGetValue(ContextConstants.EmailKey, out userName))
                {
                    searchQuery.EmailId = userName;
                }
                var response = await this.AddExpenseByEmailId(searchQuery);
                var resultMessage = context.MakeMessage();
                if (response.status == "success")
                {
                    // await context.PostAsync("I have added the expense for you");
                    resultMessage.Text = "I have added the expense for you";

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
            var responseElement = await MakeRequest<ExpenseData>("http://expensebuddy.azurewebsites.net/api/botdata/myexpense?emailId=" + userid, null, Method.GET);//


            return responseElement;
        }

        public async Task<BaseData> AddExpenseByEmailId(AddExpenseQuery data)
        {
            var dict = new Dictionary<string, object>();
            dict.Add("emailid", data.EmailId);
            dict.Add("name", data.ExpenseItemName);
            dict.Add("amount", data.Amount);
            dict.Add("description", data.Description);
            var responseElement = await MakeRequest<BaseData>("http://expensebuddy.azurewebsites.net/api/botdata/addexpense", dict, Method.POST);//


            return responseElement;
        }

        public enum Method
        {
            GET,
            POST
        }
        private async Task<T> MakeRequest<T>(string url, object postdata = null, Method method = Method.GET) where T : class 
        {
            HttpClient client = new HttpClient();
            // client.BaseAddress = new Uri("http://expensebuddy.azurewebsites.net/api/botdata/myexpense?emailId=" + userid);
            HttpResponseMessage response = null;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (method == Method.GET)
            {
                response = client.GetAsync(url).Result;
            }
            else
            {
                var jsn = JsonConvert.SerializeObject(postdata);
                var htpc = new StringContent(jsn, System.Text.Encoding.UTF8, "application/json");
                response = client.PostAsync(url, htpc).Result;
                //"http://expensebuddy.azurewebsites.net/api/botdata/myexpense?emailId=" + userid
                //"http://expensebuddy.azurewebsites.net/api/botdata/addexpense"
            }
            JsonSerializerSettings settings = new JsonSerializerSettings();

            String responseString = await response.Content.ReadAsStringAsync();

            var responseElement = JsonConvert.DeserializeObject<T>(responseString, settings);

            return responseElement;
        }
        private IForm<ExpenseQuery> BuildExpenseForm()
        {

            OnCompletionAsyncDelegate<ExpenseQuery> processHotelsSearch = async (context, state) =>

            {

                var message = "Getting  expense of ";

                if (!string.IsNullOrEmpty(state.EmailId))

                {
                    string userName;

                    if (!context.UserData.TryGetValue(ContextConstants.EmailKey, out userName))
                    {
                        context.UserData.SetValue(ContextConstants.EmailKey, state.EmailId);

                    }
                    message += $" {state.EmailId}...";

                }

                await context.PostAsync(message);

            };



            return new FormBuilder<ExpenseQuery>()

                .Field(nameof(ExpenseQuery.EmailId), (state) => string.IsNullOrEmpty(state.EmailId))
                .OnCompletion(processHotelsSearch)
                .Build();

        }
        private IForm<AddExpenseQuery> AddExpenseForm()
        {

            OnCompletionAsyncDelegate<AddExpenseQuery> addexpense = async (context, state) =>

            {

                var message = "Adding  expense for ";
                if (!string.IsNullOrEmpty(state.EmailId))

                {
                    string userName;

                    if (!context.UserData.TryGetValue(ContextConstants.EmailKey, out userName))
                    {
                        context.UserData.SetValue(ContextConstants.EmailKey, state.EmailId);

                    }


                }

                if (!string.IsNullOrEmpty(state.ExpenseItemName))

                {

                    message += $" {state.ExpenseItemName}...";

                }

                await context.PostAsync(message);

            };
            return new FormBuilder<AddExpenseQuery>()

                .Field(nameof(AddExpenseQuery.EmailId), (state) => string.IsNullOrEmpty(state.EmailId))
                .Field(nameof(AddExpenseQuery.ExpenseItemName), (state) => string.IsNullOrEmpty(state.ExpenseItemName))
                .Field(nameof(AddExpenseQuery.Amount))
                .Field(nameof(AddExpenseQuery.Description), (state) => string.IsNullOrEmpty(state.Description))
                .OnCompletion(addexpense)
                .Build();

        }
    }

}
