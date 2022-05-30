using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => 
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

app.MapPost("/send", async (EmailRequest request) =>
{
    try
    {
        if (request == null)
            throw new Exception("Request cannot be empty.");

        if (string.IsNullOrWhiteSpace(request.To))
            throw new Exception("'To' cannot be empty.");

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new Exception("'Subject cannot be empty'");

        var host = builder.Configuration.GetSection("EmailOptions:Host").Value;
        var port = int.Parse(builder.Configuration.GetSection("EmailOptions:Port").Value);
        var sender = builder.Configuration.GetSection("EmailOptions:Sender").Value;
        var password = Environment.GetEnvironmentVariable("juldhais.net_password");

        using var smtpClient = new SmtpClient();
        smtpClient.Host = host;
        smtpClient.Port = port;
        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtpClient.UseDefaultCredentials = false;
        smtpClient.Credentials = new NetworkCredential(sender, password);
        smtpClient.EnableSsl = true;

        var message = new MailMessage();
        message.From = new MailAddress(sender);
        message.To.Add(request.To);
        message.Subject = request.Subject;
        message.Body = request.Body;
        message.IsBodyHtml = true;

        await smtpClient.SendMailAsync(message);

        return new EmailResponse(true, $"Email sent to {request.To}");
    }
    catch (Exception ex)
    {
        return new EmailResponse(false, ex.GetBaseException().Message);
    }
});

app.Run();

internal record EmailRequest(string To, string Subject, string Body);
internal record EmailResponse(bool Success, string Message);