using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace LuisBot.Model
{
    public static class RegexConstants
    {
        public const string Email = @"[a-z0-9!#$%&'*+\/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+\/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?";

        public const string Phone = @"^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$";
    }
    [Serializable]
    public class ExpenseQuery
    {

        [Prompt("Please enter your {&}")]

        [Pattern(RegexConstants.Email)]
        public string EmailId { get; set; }

    }
    [Serializable]
    public class AddExpenseQuery
    {

        [Prompt("Please enter your {&}")]
        [Pattern(RegexConstants.Email)]

        [DataMember(Name = "emailid")]
        public string EmailId { get; set; }

        [Prompt("Please enter the {&}")]
        [DataMember(Name = "name")]
        public string ExpenseItemName { get; set; }

        [Prompt("Please enter the {&}")]
        [DataMember(Name = "amount")]

        public double Amount { get; set; }

        [Prompt("Please enter the {&}")]
        [DataMember(Name = "description")]

        public string Description { get; set; }



    }

    [Serializable]
    public class ExpenseData
    {

        public double amount { get; set; }

        public bool status { get; set; }

        //{"amount":2908.0,"status":true}

    }
    [Serializable]
    public class BaseData
    {
        public string status { get; set; }

        public string code { get; set; }

        public string description { get; set; }

    }
}