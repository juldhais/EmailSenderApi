using System.Net;
using System.Net.Mail;

var builder = WebApplication.CreateBuilder(args);

// CORS configuration
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => 
        policy.AllowAnyHeader()
            .AllowAnyMethod()
            .AllowAnyOrigin()));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();

// end point yang digunakan untuk mengirim email
// method: POST
// route: send
// https://localhost:5000/send
app.MapPost("/send", async (EmailRequest request) =>
{
    try
    {
        
        // validasi request model
        if (request == null)
            throw new Exception("Request cannot be empty.");

        if (string.IsNullOrWhiteSpace(request.To))
            throw new Exception("'To' cannot be empty.");

        if (string.IsNullOrWhiteSpace(request.Subject))
            throw new Exception("'Subject cannot be empty'");

        // mengambil konfigurasi email dari appsettings.json
        var host = builder.Configuration.GetSection("EmailOptions:Host").Value;
        var port = int.Parse(builder.Configuration.GetSection("EmailOptions:Port").Value);
        var sender = builder.Configuration.GetSection("EmailOptions:Sender").Value;
        
        // mengambil password dari environment variable
        var password = Environment.GetEnvironmentVariable("EmailOptionsPassword");

        // konfigurasi SmtpClient
        using var smtpClient = new SmtpClient();
        smtpClient.Host = host;
        smtpClient.Port = port;
        smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
        smtpClient.UseDefaultCredentials = false;
        smtpClient.Credentials = new NetworkCredential(sender, password);
        smtpClient.EnableSsl = true;

        // mengatur From, To, Subject, dan Body dari email yang akan dikirim
        var message = new MailMessage();
        message.From = new MailAddress(sender);
        message.To.Add(request.To);
        message.Subject = request.Subject;
        message.Body = request.Body;
        message.IsBodyHtml = true;

        // mengirim email
        await smtpClient.SendMailAsync(message);

        // mengembalikan response success = true jika proses pengiriman email berhasil
        return new EmailResponse(true, $"Email sent to {request.To}");
    }
    catch (Exception ex)
    {
        // mengirimkan response success = false jika gagal
        return new EmailResponse(false, ex.GetBaseException().Message);
    }
});

app.Run();


// request model
public record EmailRequest(string To, string Subject, string Body);

// response model
public record EmailResponse(bool Success, string Message);