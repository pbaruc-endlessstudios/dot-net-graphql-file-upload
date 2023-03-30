using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Net;
using System.Security.Cryptography.X509Certificates;



// GraphQL Client to the development environment
var graphQLClient = new GraphQLHttpClient("http://localhost:3003/graphql", new NewtonsoftJsonSerializer());

// Local variables
var fileName = "somefile.txt";
var fileName2 = "somefile.v2.txt";
var mime_type = "text/plain";
var size = 100; // Hardcoded here but you should calculate the real size of the file.


// ===================================================================================================================================
// 1: Login
// ===================================================================================================================================

// Create a GraphQL Request that invokes the loginWithUsernameOrEmail graphql mutation
// pass in a username or email and a password and recieve back and auth token.
var loginWithUsernameOrEmail = new GraphQLRequest
{
Query = @"mutation loginWithUsernameOrEmail($username_or_email: String!, $password: String!) {
        loginWithUsernameOrEmail(input: {
            username_or_email: $username_or_email
            password: $password
        })
    }",
    OperationName = "loginWithUsernameOrEmail",
    Variables = new
    {
        username_or_email = "user0",    // This is an existing test account we have establised in dev environment
        password = "12345678"
    }
};

// Invoke the query against service, retrieve response
var graphQLResponse = await graphQLClient.SendQueryAsync<LoginWithUsernameOrEmailResponseType>(loginWithUsernameOrEmail);

// Extract the authToken from the the response like this... 
var authToken = graphQLResponse.Data.loginWithUsernameOrEmail;

// Now store the authToken into an 'Authorization' request header in graphQLClient...
graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + authToken);

// ===================================================================================================================================
// 2: Invoke mutation createFileUploadLink to generate a secure s3 url and file_instance_id
// ===================================================================================================================================
var fileUploadLink = new GraphQLRequest
{
    Query = @"mutation createFileUploadLink($name: String!, $mime_type: String!, $size: Int!) {
        createFileUploadLink(input: {
            name: $name
            mime_type: $mime_type
            size: $size
        }) {
            secure_upload_url
            additional_s3_security_fields
            file_id
            file_instance_id
        }
    }",
    OperationName = "createFileUploadLink",
    Variables = new {
        name = fileName,
        mime_type = mime_type,
        size = size
    }
};

// Invoke createFileUploadLink mutation, this will generate file instance id and a secure s3 url where you can upload your file.
var fileUpLoadLinkGraphQLResponse = await graphQLClient.SendQueryAsync<FileUploadLinkResponseType>(fileUploadLink);
Console.WriteLine("Response from graphql-server for \"createFileUploadLink\" resolver: " + Newtonsoft.Json.JsonConvert.SerializeObject(fileUpLoadLinkGraphQLResponse.Data));

// ===================================================================================================================================
// 3: Upload the file using some httpClient, the secure s3 url, and additional_s3_fields from fileUploadLink response.
// ===================================================================================================================================

// Use httpClient to form post multipart to the secure s3 url
using (var client = new HttpClient())
{
    try {

        // extract the secure upload url
        var url = fileUpLoadLinkGraphQLResponse.Data.createFileUploadLink.secure_upload_url;

        // extract the additionalS3SecurityFields and deserialize into a Dictionary
        var additionalS3SecurityFields = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(fileUpLoadLinkGraphQLResponse.Data.createFileUploadLink.additional_s3_security_fields);

        // establish multipartFormDataContent object
        using(var multipartFormDataContent = new MultipartFormDataContent())
        {

            // add all the additionalS3Security fields into the multipartFormDataContent object
            foreach(var additionalHeaderField in additionalS3SecurityFields)
            {
                multipartFormDataContent.Add(new StringContent(additionalHeaderField.Value), additionalHeaderField.Key);
            }

            // add the actual file to upload and make sure to name the field name is 'file'
            multipartFormDataContent.Add(new ByteArrayContent(File.ReadAllBytes(fileName)), "file", fileName);

            // post to the url
            var result = client.PostAsync(url, multipartFormDataContent).Result;

            // capture the response.
            Console.Out.WriteLine("Response: " + result);
        }

    } catch(Exception e) {
        Console.Out.WriteLine(e);
    }
}

// ===================================================================================================================================
// 4: Flag the file_instance as uploaded so backend knows that the upload was successful
// ===================================================================================================================================
var markFileInstanceUploaded = new GraphQLRequest
{
    Query = @"mutation markFileInstanceUploaded($file_instance_id: Int!) {
        markFileInstanceUploaded(input: {
            file_instance_id: $file_instance_id
        })
    }",
    OperationName = "markFileInstanceUploaded",
    Variables = new {
        file_instance_id = fileUpLoadLinkGraphQLResponse.Data.createFileUploadLink.file_instance_id,
    }
};
// Invoke markeFileUploadedResponse
var markFileInstanceUploadedGraphQLResponse = await graphQLClient.SendQueryAsync<MarkFileInstanceUploadedResponseType>(markFileInstanceUploaded);
Console.WriteLine("Response from graphql-server for \"markFileInstanceUploaded\" resolver: " + Newtonsoft.Json.JsonConvert.SerializeObject(markFileInstanceUploadedGraphQLResponse.Data));

// ===================================================================================================================================
// 5: Create the asset passing in the file_instance_id from above
// ===================================================================================================================================
var createAsset = new GraphQLRequest
{
    Query = @"mutation createAsset($asset_id: String, $asset_type: String, $asset_file_instance_id: Int) {
        createAsset(input: {
            asset: {
                asset_id: $asset_id,
                asset_type: $asset_type,
                asset_file_instance_id: $asset_file_instance_id
            }
        })
    }",
    OperationName = "createAsset",
    Variables = new {
        asset_id = "my-unique-asset-id-" + DateTime.Now.Ticks,
        asset_type = "my-asset-type",
        asset_file_instance_id = fileUpLoadLinkGraphQLResponse.Data.createFileUploadLink.file_instance_id // NOTE: The file_instance_id from the createFileUploadLink response.
    }
};

// Invoke createAsset
var createAssetGraphQLResponse = await graphQLClient.SendQueryAsync<CreateAssetResponseType>(createAsset);
Console.WriteLine("Response from graphql-server for \"createAsset\" resolver: " + Newtonsoft.Json.JsonConvert.SerializeObject(createAssetGraphQLResponse.Data));

// ===================================================================================================================================
// 6: Get the AssetByIdAndVersion, you will see a new fields named asset_file_url, e.g.:
// {
//   "data": {
//     "getAssetByIdAndVersion": {
//       "asset_id": "my-unique-asset-id-001",
//       "asset_version": "0.0.1",
//       "asset_type": "my-asset-type",
//       "asset_file_instance_id": 1320,
//       "asset_file_url": "https://endlessstudios-dev-api-images.s3.us-east-2.amazonaws.com/task_progress/file_instance/somefile.txt/BNjBJZvxNxv?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Credential=AKIAQLBMIV2Q73QV4ED4%2F20230330%2Fus-east-2%2Fs3%2Faws4_request&X-Amz-Date=20230330T224756Z&X-Amz-Expires=86400&X-Amz-Signature=41a74434a1e6ad7713d213390d7c955ead07093aa49fd982c89afefa6f1661a7&X-Amz-SignedHeaders=host&x-id=GetObject"
//     }
//   }
// }
// ===================================================================================================================================
var getAssetByIdAndVersion = new GraphQLRequest
{
    Query = @"query getAssetByIdAndVersion($asset_id: String, $asset_version: String) {
        getAssetByIdAndVersion(input: {
            asset_id: $asset_id,
            asset_version: $asset_version,
        })
    }",
    OperationName = "getAssetByIdAndVersion",
    Variables = new {
        asset_id = createAssetGraphQLResponse.Data.createAsset.asset_id,
        asset_version = createAssetGraphQLResponse.Data.createAsset.asset_version,
    }
};

// Invoke getAsset
var getAssetByIdAndVersionGraphQLResponse = await graphQLClient.SendQueryAsync<GetAssetByIdAndVersionTypeResponseType>(getAssetByIdAndVersion);
Console.WriteLine("Response from graphql-server for \"getAssetByIdAndVersion\" resolver: " + Newtonsoft.Json.JsonConvert.SerializeObject(getAssetByIdAndVersionGraphQLResponse.Data));


// ===== Response Types ============================================================================
public class LoginWithUsernameOrEmailResponseType
{
    public string loginWithUsernameOrEmail { get; set; }
}

public class MeType
{
    public int id { get; set; }

    public string username { get; set; }

    public string email { get; set; }

}

public class MeResponseType
{
    public MeType me { get; set; }
}

public class FileUploadLinkType
{
    public string secure_upload_url { get; set; }

    public string additional_s3_security_fields { get; set; }

    public int file_id { get; set; }

    public int file_instance_id { get; set; }
}

public class FileUploadLinkResponseType
{
    public FileUploadLinkType createFileUploadLink { get; set; }
}

public class AssetType {
    public string asset_id { get; set; }
    public string asset_version { get; set; }
    public string asset_type  { get; set; }
    public string asset_file_instance_id { get; set; }
    public string asset_file_url { get; set; }

}
public class CreateAssetResponseType
{
    public AssetType createAsset { get; set; }
}

public class GetAssetByIdAndVersionTypeResponseType {
    public AssetType getAssetByIdAndVersion { get; set; }
}


public class MarkFileInstanceUploadedResponseType {
    public string markFileInstanceUploaded { get; set; }
}



