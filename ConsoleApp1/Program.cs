using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Security.Cryptography.X509Certificates;


// GraphQL Client to the development environment
var graphQLClient = new GraphQLHttpClient("https://dev.graphql-api.endlessstudios.com/graphql", new NewtonsoftJsonSerializer());


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

// ====================================================================
// Now all subsequent queries will execute queries using the auth token
// ====================================================================

// Create graphql query to return details for the authorized user.
var me = new GraphQLRequest
{
    Query = @"query me {
        me {
            id
            username
            email
        }
    }",
    OperationName = "me"
};
// Invoke the query against service, retrieve response
var meGraphQLResponse = await graphQLClient.SendQueryAsync<MeResponseType>(me);
Console.WriteLine("Me: " + Newtonsoft.Json.JsonConvert.SerializeObject(meGraphQLResponse.Data));



// ================================================================================================================

public class LoginWithUsernameOrEmailResponseType
{
    public string loginWithUsernameOrEmail { get; set; }
}


public class Metype
{
    public int id { get; set; }

    public string username { get; set; }

    public string email { get; set; }

}
public class MeResponseType
{
    public Metype me { get; set; }
}

