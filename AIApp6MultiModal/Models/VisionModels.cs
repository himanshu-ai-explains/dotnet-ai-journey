namespace AIApp6MultiModal.Models
{
    public class AnalysisResult
    {
        public string Description { get; set; } = string.Empty;
        public string ExtractedText { get; set; } = string.Empty;
        public List<string> DetectedItems { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string FileName { get; set; } = string.Empty;
        public int TokenUsed { get; set; }

    }

    public class QuestionResult
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;

    }

    public class InvoiceData
    {
        public string VendorName { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string InvoiceDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public List<InvoiceLineItem> LineItems { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal TotalTax { get; set; }
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "INR";
        public string Notes { get; set; } = string.Empty;
    }

    public class InvoiceLineItem
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Total { get; set; }
    }
}
