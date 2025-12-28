using BackEndFolio.API.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;


var builder = WebApplication.CreateBuilder(args);

var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:Key"];

// A. Đăng ký Supabase Client (Scoped: Mỗi request tạo 1 cái mới)
builder.Services.AddScoped<Supabase.Client>(_ =>
    new Supabase.Client(supabaseUrl, supabaseKey, new Supabase.SupabaseOptions
    {
        AutoRefreshToken = true,
        AutoConnectRealtime = true
    }));



// B. Cấu hình JWT Authentication 
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 1. Quan trọng: Chỉ định Authority (Nơi cấp phát Token)
        // .NET sẽ tự động gọi vào {Authority}/.well-known/openid-configuration để lấy Public Key
        options.Authority = $"{supabaseUrl}/auth/v1";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // Bỏ dòng ValidateIssuerSigningKey và IssuerSigningKey cũ đi

            ValidateIssuer = true, // Nên bật true vì ta đã có Authority chuẩn
            ValidIssuer = $"{supabaseUrl}/auth/v1", // Token phải do đúng URL này cấp

            ValidateAudience = false, // Supabase JWT thường không có audience cố định cho API riêng

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

         // Cấu hình để SignalR nhận được Token (Vì SignalR gửi token qua QueryString chứ không phải Header)
        options.Events = new JwtBearerEvents
        { 
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // Nếu đường dẫn bắt đầu bằng /appHub thì lấy token từ query
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/appHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// C. Đăng ký Controllers + Cấu hình JSON (Quan trọng!)
//builder.Services.AddControllers()
//    .AddJsonOptions(options =>
//    {
//        // Bỏ qua lỗi vòng lặp (Circular Reference) khi join bảng
//        // Ví dụ: User -> Project -> User -> ...
//        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
//        options.JsonSerializerOptions.WriteIndented = true;
//    });

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        // Cấu hình để bỏ qua vòng lặp (nếu cần) bằng Newtonsoft
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;

        // (Tùy chọn) Bỏ qua giá trị null cho gọn
        options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
    });

// D. Đăng ký SignalR (Realtime)
builder.Services.AddSignalR();

// E. Cấu hình CORS (Cho phép Frontend gọi vào)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") // Thay đổi port React của bạn nếu khác
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Bắt buộc phải có dòng này để SignalR chạy được
    });
});



builder.Services.AddEndpointsApiExplorer();


var app = builder.Build();



// Môi trường Dev thì bật Swagger
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 1. Kích hoạt CORS (Phải đặt trước Auth)
app.UseCors("AllowFrontend");

// 2. Kích hoạt Routing
app.UseRouting();


app.UseAuthentication();
app.UseAuthorization();


app.MapControllers(); // Map các API Controller


app.MapHub<AppHub>("/appHub"); // Map đường dẫn cho SignalR

app.Run();