using System.Data;
using Dapper;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);


builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();


builder.Services
     .AddScoped<IDbConnection>(sp =>
     {
         var connection = new MySqlConnection(sp.GetRequiredService<IConfiguration>().GetConnectionString("MySQL"));
         connection.Open();

         return connection;
     });



var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();



app.MapGet("/connection", (IConfiguration configuration) =>
    TypedResults.Ok(configuration.GetConnectionString("MySQL")));


app.MapGet("/products", async (IDbConnection connection) =>
{
    var result = await connection.QueryAsync<ProductResponse>(
        """
        SELECT
            `Id`,
            `Name`,
            `Quantity`
        FROM `Product` ;
        """);

    return TypedResults.Ok(result);
});


app.MapGet("/products/{id:int}", async (IDbConnection connection, uint id) =>
{
    var result = await connection.QuerySingleOrDefaultAsync<ProductResponse>(
        """
        SELECT
            `Id`,
            `Name`,
            `Quantity`
        FROM `Product`
        WHERE `id` = @id ;
        """,
        new { id });

    return TypedResults.Ok(result);
}).WithName("GetProduct");


app.MapPost("/products", async (IDbConnection connection, ProductRequest product) =>
{
    var id = await connection.ExecuteScalarAsync<ulong>(
        """
        INSERT `Product` (`Name`, `Quantity`)
                    VALUE (@Name , @Quantity );
        SELECT LAST_INSERT_ID();
        """,
        product);

    return TypedResults.CreatedAtRoute(
        id,
        "GetProduct",
        new { id });
});


app.Run();



public record ProductRequest
{
    public string? Name { get; init; }
    public int Quantity { get; init; }
};


public record ProductResponse
{
    public uint Id { get; init; }
    public string? Name { get; init; }
    public int Quantity { get; init; }
};
