using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;

namespace cnalyzer
{
    public partial class MainWindow : Window
    {
        // Replace with your actual Gemini API Key
        private const string ApiKey = "AIzaSyBrk0fN3-eOahgpFU3db0rR5aQDIXZQ2bo";
        private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            BtnAnalyze.IsEnabled = false;
            TxtStatus.Text = "AI is analyzing data and searching news...";
            ResultPanel.Visibility = Visibility.Collapsed;

            try
            {
                string symbol = TxtSymbol.Text;
                string timeframe = ComboTimeframe.Text;
                string rsi = TxtRsi.Text;
                bool useFundamentals = ChkFundamentals.IsChecked ?? false;

                // Create the prompt for Gemini
                string prompt = $"Analyze the cryptocurrency {symbol} on the {timeframe} timeframe. " +
                                $"The current RSI is {rsi}. " +
                                (useFundamentals ? "Search for and include recent fundamental news/sentiment in your analysis. " : "") +
                                "Provide a recommendation (BUY, SELL, or HOLD), a confidence percentage (0-100), and a brief reasoning.";

                string result = await GetGeminiResponse(prompt);
                DisplayResult(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
                TxtStatus.Text = "Analysis failed.";
            }
            finally
            {
                BtnAnalyze.IsEnabled = true;
            }
        }

        private async Task<string> GetGeminiResponse(string prompt)
        {
            using var client = new HttpClient();
            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{ApiUrl}?key={ApiKey}", content);
            response.EnsureSuccessStatusCode();

            string responseJson = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(responseJson);

            // Extract the text content from the Gemini JSON response
            return doc.RootElement.GetProperty("candidates")[0]
                                  .GetProperty("content")
                                  .GetProperty("parts")[0]
                                  .GetProperty("text").GetString() ?? "No response";
        }

        private void DisplayResult(string rawAiOutput)
        {
            TxtStatus.Text = "Analysis Complete";
            ResultPanel.Visibility = Visibility.Visible;
            TxtReasoning.Text = rawAiOutput;

            // Simple parser logic (You can make this more robust)
            if (rawAiOutput.ToUpper().Contains("BUY"))
            {
                TxtRecommendation.Text = "BUY";
                TxtRecommendation.Foreground = Brushes.LightGreen;
            }
            else if (rawAiOutput.ToUpper().Contains("SELL"))
            {
                TxtRecommendation.Text = "SELL";
                TxtRecommendation.Foreground = Brushes.Salmon;
            }
            else
            {
                TxtRecommendation.Text = "HOLD";
                TxtRecommendation.Foreground = Brushes.Yellow;
            }

            // Mock confidence parsing (In a real app, ask AI to return JSON)
            ProgressConfidence.Value = 75;
            TxtConfidence.Text = "75%";
        }
    }
}