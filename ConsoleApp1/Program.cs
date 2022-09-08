using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Security.Cryptography.X509Certificates;

var graphQLClient = new GraphQLHttpClient("https://dev.graphql-api.endlessstudios.com/graphql", new NewtonsoftJsonSerializer());
/*graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer")
*/
/*var loginWithUsernameOrEmail = new GraphQLRequest
{
Query = @"query Check {
        check {
            systemHealthy
        }
    }",
OperationName = "Check"
};




var graphQLResponse = await graphQLClient.SendQueryAsync<ResponseType>(loginWithUsernameOrEmail);

var token = graphQLResponse.Data;

Console.WriteLine("Token: " + token);

public class CheckType
{
    public bool systemHealthy { get; set; }
}
public class ResponseType
{
    public CheckType check { get; set; }
}
*/


/*public class CheckType
{
    public bool systemHealthy { get; set; }
}
public class ResponseType
{
    public CheckType check { get; set; }
}
*/

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
        username_or_email = "user0",
        password = "12345678"
    }
};




var graphQLResponse = await graphQLClient.SendQueryAsync<ResponseType>(loginWithUsernameOrEmail);

var token = graphQLResponse.Data.loginWithUsernameOrEmail;

Console.WriteLine("Token: " + token);

public class ResponseType
{
    public string loginWithUsernameOrEmail { get; set; }
}
