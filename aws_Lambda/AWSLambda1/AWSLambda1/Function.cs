using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AWSLambda1;

public class Function
{
    //public string FunctionHandler(string input, ILambdaContext context)
    //{
    //    return input.ToUpper();
    //}

    //public CreateProductResult FunctionHandler(CreateProductRequest input, ILambdaContext context) 
    //{
    //    context.Logger.LogLine("Hello first lamda function");

    //    // Assume Product is Saved to DB
    //    var response = new CreateProductResponse();
    //    response.ProductId = Guid.NewGuid().ToString();

    //    // केवल JSON Body से डेटा लें
    //    response.Name = input.Name;
    //    response.Description = input.Description;

    //    return new CreateProductResult
    //    {
    //        StatusCode = 200,
    //        Response = response
    //    };
    //}
  
    private const string LayerFilePath = "/opt/lamda_code.txt";

    public CreateProductResult FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        context.Logger.LogLine("Hello first lamda function");

        string fileContent = "File not found.";

        context.Logger.LogLine("Starting /opt directory diagnostic...");

        try
        {
            if (Directory.Exists("/opt"))
            {
                // /opt के अंदर की सभी फाइलों को list करें
                string[] allFiles = Directory.GetFiles("/opt", "*", SearchOption.AllDirectories);
                context.Logger.LogLine($"Total files found in /opt: {allFiles.Length}");
                foreach (var file in allFiles)
                {
                    // यह लाइन आपको सही path बताएगी!
                    context.Logger.LogLine($"Found file at absolute path: {file}");
                }
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"Diagnostic Error: {ex.Message}");
        }

        // 💡 Layer से text file पढ़ने का Code
        try
        {
            // File.Exists से जांचें कि file /opt/ directory में मौजूद है या नहीं
            if (File.Exists(LayerFilePath))
            {
                // File.ReadAllText से content पढ़ें
                fileContent = File.ReadAllText(LayerFilePath);
                context.Logger.LogLine($"Successfully read file from Layer. Content: {fileContent}");
            }
            else
            {
                context.Logger.LogLine("ERROR: Layer file not found at /opt/lamda_code.txt");
            }
        }
        catch (Exception ex)
        {
            context.Logger.LogLine($"ERROR reading file from Layer: {ex.Message}");
        }
            // Query String से डेटा पढ़ें
            string urlName = request.QueryStringParameters?["name"];
        string urlDesc = request.QueryStringParameters?["description"];

        // A. JSON Body को डी-सीरियललाइज़ करें
        CreateProductResponse bodyData = null; // **ध्यान दें: हमने टाइप को CreateProductResponse में बदल दिया है**
        if (bodyData == null)
        {
            bodyData = new CreateProductResponse();
        }
 
        if (!string.IsNullOrEmpty(request.Body))
        {
            try
            {
                // JSON Body को अपनी C# क्लास CreateProductResponse में बदलें
                bodyData = JsonConvert.DeserializeObject<CreateProductResponse>(request.Body);
                bodyData.ProductId = Guid.NewGuid().ToString(); 
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error deserializing body: {ex.Message}");
                // यदि JSON Body अमान्य (invalid) है, तो एक त्रुटि रिस्पांस भेजें
                return new CreateProductResult
                {
                    StatusCode = 400, // Bad Request
                    Response = new CreateProductResponse { Name = $"Error: Invalid JSON Body: {ex.Message}" }
                };
            }
        }

        // B. Query String और Body डेटा को मर्ज करें

        // finalName: अगर URL में 'name' है, तो उसे उपयोग करें, अन्यथा bodyData.Name उपयोग करें।
        string finalName = urlName ?? bodyData?.Name;

        // finalDescription: अगर URL में 'description' है, तो उसे उपयोग करें, अन्यथा bodyData.Description उपयोग करें।
        string finalDescription = urlDesc ?? bodyData?.Description;


        // C. रिस्पांस ऑब्जेक्ट तैयार करें
        var response = new CreateProductResponse
        {
            ProductId = Guid.NewGuid().ToString(),
            Name = finalName,
            Description = finalDescription
        };

        // D. परिणाम वापस करें
        return new CreateProductResult
        {
            StatusCode = 200,
            Response = response
        };
    }


    public class CreateProductResponse
    {
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class CreateProductResult
    {
        public int StatusCode { get; set; }
        public CreateProductResponse Response { get; set; }
    }
}
