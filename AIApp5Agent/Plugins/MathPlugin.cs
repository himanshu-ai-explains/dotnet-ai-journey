using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace AIApp5Agent.Plugins
{
    public class MathPlugin
    {
        [KernelFunction]
        [Description("Evaluates a mathematical expression and returns the result. Examples: '150 * 0.18', '2500 / 4', '(100 + 200) * 3'\")")]
        public string Calculate(
             [Description("The mathematical expression to evaluate, e.g. '150 * 0.18'")] string expression)

        {
            try
            {
                var table = new System.Data.DataTable();
                var result = table.Compute(expression, null);
                return $"Result of {expression} = {result}";
            }
            catch 
            {

                return $"Could not calculate '{expression}'. Please use a simple math expression like '100 * 0.18'";
            }
        }

        [KernelFunction]
        [Description("Calculates a percentage tip of a bill amount in Indian Rupees")]

        public string CalculateTip(
                    [Description("The bill amount in rupees")] double billAmount,
                    [Description("The tip percentage, e.g. 10 for 10%")] double tipPercent)

        {
            double tip = billAmount * (tipPercent / 100);
            double total = billAmount + tip;
            return $"Bill: ₹{billAmount:F2} | Tip ({tipPercent}%): ₹{tip:F2} | Total: ₹{total:F2}";
        }


        [KernelFunction]
        [Description("Converts temperature between Celsius and Fahrenheit")]
        public string ConvertTemperature(
        [Description("The temperature value to convert")] double value,
        [Description("Either 'celsius' or 'fahrenheit'")] string fromUnit)
        {
            if (fromUnit.ToLower() == "celsius")
            {
                double f = (value * 9 / 5) + 32;
                return $"{value}°C = {f:F1}°F";
            }
            else
            {
                double c = (value - 32) * 5 / 9;
                return $"{value}°F = {c:F1}°C";
            }
        }

        [KernelFunction]
        [Description("Converts USD to Indian Rupees using approximate rate")]
        public string ConvertUsdToInr(
            [Description("Amount in USD to convert")] double usdAmount)
        {
            double rate = 83.5; // approximate rate
            double inr = usdAmount * rate;
            return $"${usdAmount} USD ≈ ₹{inr:F2} INR (at approximate rate of ₹{rate}/USD)";
        }


    }
}
