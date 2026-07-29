using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace AIApp5Agent.Plugins;

// IndiaInfoPlugin: domain-specific knowledge plugin
// Shows how you can give the agent custom business/domain knowledge
// In real apps this would query a database or external API
public class IndiaInfoPlugin
{
    [KernelFunction]
    [Description("Gets information about the tech and AI ecosystem in major Indian cities")]
    public string GetCityTechInfo(
        [Description("The Indian city name, e.g. Delhi, Mumbai, Bangalore, Hyderabad, Pune")] string city)
    {
        var info = city.ToLower() switch
        {
            "delhi" or "new delhi" => "Delhi NCR: Massive enterprise IT hub. Strong in government tech, fintech, and edtech. Growing AI startup scene in Gurugram and Noida. Major employers: TCS, Wipro, HCL (HQ), Adobe, Microsoft India. AI talent demand is very high — especially for Azure and .NET AI developers.",
            "bangalore" or "bengaluru" => "Bangalore: India's Silicon Valley. Highest density of AI startups. Home to Amazon, Google, Microsoft, and Flipkart engineering centres. Strongest demand for ML engineers and AI application developers. Best city for AI career growth in India.",
            "hyderabad" => "Hyderabad: Major Microsoft and Google campuses here. Strong in cloud and AI. HITEC City is the tech district. Growing faster than Bangalore for AI-focused roles. Cost of living lower than Bangalore.",
            "mumbai" => "Mumbai: Fintech and media tech capital. Strong AI adoption in banking and finance. Major financial institutions all running AI projects. TCS headquarters here.",
            "pune" => "Pune: Strong engineering talent pool. Many product companies. Good for .NET and Java enterprise work with growing AI integration roles.",
            "chennai" => "Chennai: Strong in automotive tech and manufacturing AI. Zoho is based here. Growing enterprise AI market.",
            _ => $"{city}: Part of India's rapidly growing tech ecosystem. AI and cloud skills are in high demand across all Indian tech markets."
        };

        return info;
    }

    [KernelFunction]
    [Description("Gets the current state of the AI job market in India for .NET developers")]
    public string GetAIJobMarketInfo()
    {
        return """
            AI Job Market in India (2026):
            
            DEMAND: Very high — every enterprise is building AI features
            
            SALARY RANGES:
            - .NET Developer with AI skills: ₹20-45 LPA
            - Senior AI Engineer (.NET + Azure): ₹40-80 LPA  
            - AI Architect: ₹70-130 LPA
            
            MOST WANTED SKILLS:
            1. Azure OpenAI + Semantic Kernel
            2. RAG pipeline implementation
            3. AI Agent development
            4. Vector databases (Azure AI Search, Qdrant)
            5. Prompt engineering
            
            TOP HIRING CITIES: Bangalore, Hyderabad, Delhi NCR, Pune
            
            TOP HIRING COMPANIES: Microsoft, Accenture, Infosys, TCS, 
            Wipro, HCL, startups in fintech and edtech
            
            ADVANTAGE: .NET + AI combination is rare and highly valued
            """;
    }

    [KernelFunction]
    [Description("Gets information about Microsoft AI certifications relevant to .NET developers in India")]
    public string GetCertificationInfo()
    {
        return """
            Microsoft AI Certifications for .NET Developers:
            
            1. AI-900 (Azure AI Fundamentals)
               - Difficulty: Easy | Exam fee: ₹3,600
               - Study time: 2-3 weeks
               - Best starting certification
            
            2. AZ-204 (Azure Developer Associate)  
               - Difficulty: Medium | Exam fee: ₹4,800
               - Covers Azure AI services integration
               - Great for .NET developers
            
            3. AI-102 (Azure AI Engineer Associate)
               - Difficulty: Hard | Exam fee: ₹4,800
               - The most valuable AI cert for your profile
               - Covers Cognitive Services, OpenAI, Search
            
            Recommended order: AI-900 → AZ-204 → AI-102
            """;
    }
}