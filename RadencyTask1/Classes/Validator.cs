using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RadencyTask1.Classes
{
    public class Validator
    {
        public string ToValidate { get; set; }

        public Validator(string toValidate)
        {
            this.ToValidate = toValidate;
        }

        public bool Validate(ref DataRow dataRow)
        {
            if(FirstValidate()) if(SecondValidate(ref dataRow)) return true;

            return false;
        }

        private bool FirstValidate()
        {
            string pattern = @"[A-Za-z]+(, +|,)[A-Za-z]+(, +|,)(""|'|“|`)[A-Za-z]+(, +|,)[A-Za-z]+ [0-9A-Za-z/\\]+(, +|,)\d+(""|'|”|`)(, +|,)\d+\.\d+(, +|,)\d{4}-\d{1,2}-\d{1,2}(, +|,)\d+, [A-Za-z]+";
            Regex regex = new Regex(pattern);
            bool isValid = regex.IsMatch(ToValidate);
            return isValid;
        }
        private bool SecondValidate(ref DataRow dataRow)
        {
            ToValidate = Regex.Replace(ToValidate, @"[^\w\d\.\n\-]+", "|");
            List<string> separated = ToValidate.Split('|').ToList();

            if(ValidateName(separated))
            {
                decimal payment;
                DateTime date;
                long accountNum;
                try
                {
                    payment = decimal.Parse(separated[6], System.Globalization.CultureInfo.InvariantCulture);
                    date = DateTime.ParseExact(separated[7], "yyyy-dd-MM", System.Globalization.CultureInfo.InvariantCulture);
                    accountNum = long.Parse(separated[8]);

                    dataRow.Name = $"{separated[0]} {separated[1]}";
                    dataRow.City = $"{separated[2]}";
                    dataRow.Payment = payment;
                    dataRow.Date = date;
                    dataRow.AccountNumber = accountNum;
                    dataRow.Service = separated[9];
                } catch(Exception e)
                {
                    return false;
                }
            }

            return true;
        }
        private bool ValidateName(List<string> separated)
        {
            bool isValidName = false;
            if(!String.IsNullOrWhiteSpace(separated[0]) 
                || !String.IsNullOrWhiteSpace(separated[1])) isValidName = true;
            return isValidName;
        }
    }
}
