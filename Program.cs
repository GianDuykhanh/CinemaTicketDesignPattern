using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using movieCinema.Data.Cart;
using movieCinema.Data.Facade;
using movieCinema.Data.Mediator;
using movieCinema.Data.Observer;
using movieCinema.Data.Proxy;
using movieCinema.Data.Services;
using movieCinema.Data.Strategy;
using movieCinema.Models;
using movieCinema.Models.Bridge;
using movieCinema.Models.Builders;
using MovieCinema.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// DbContext Configuration
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Services Configuration
builder.Services.AddScoped<IActorsService, ActorsService>();
builder.Services.AddScoped<IProducersService, ProducersService>();
builder.Services.AddScoped<ICinemasService, CinemasService>();
builder.Services.AddScoped<ICinemaRoomsService, CinemaRoomsService>();
builder.Services.AddScoped<ICategoriesService, CategoriesService>();
builder.Services.AddScoped<IOrdersService, OrdersService>();
builder.Services.AddScoped<ISeatsService, SeatsService>();
builder.Services.AddScoped<IShowtimesService, ShowtimesService>();
builder.Services.AddScoped<IVouchersService, VouchersService>();

// MoviesService must be registered BEFORE the proxy
builder.Services.AddScoped<MoviesService>();

// ── Design Patterns — Phase 1 ───────────────────────────────────────────────
// Bridge (Seat Pricing)
builder.Services.AddScoped<ISeatingPricingStrategy, StandardPricingStrategy>();

// Builder
builder.Services.AddScoped<IOrderBuilder, OrderBuilder>();

// Strategy (Payment)
builder.Services.AddScoped<IPaymentStrategy, CashPaymentStrategy>();

// Facade
builder.Services.AddScoped<IBookingFacade, BookingFacade>();

// ── Design Patterns — Phase 2 ───────────────────────────────────────────────
// State: Already integrated in OrdersService.ChangeOrderStatusWithStateAsync()

// ── Design Patterns — Phase 3 ───────────────────────────────────────────────
// Decorator: Already used in BookingFacade via OrderPriceCalculator

// Proxy: Cache layer for MoviesService (wraps MoviesService)
builder.Services.AddScoped<IMoviesService>(sp =>
{
    var realService = sp.GetRequiredService<MoviesService>();
    var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    return new CachedMoviesServiceProxy(realService, cache);
});

// Observer pattern
builder.Services.AddSingleton<IOrderSubject, OrderSubject>();
builder.Services.AddScoped<IOrderObserver, AuditLogObserver>();
builder.Services.AddScoped<IOrderObserver, LoyaltyPointsObserver>();
builder.Services.AddScoped<IOrderObserver, EmailNotificationObserver>();

// ── Design Patterns — Phase 4 ───────────────────────────────────────────────
// Mediator
builder.Services.AddScoped<IMediator, AppMediator>();
builder.Services.AddScoped<IRequestHandler<CompleteBookingRequest, CompleteBookingResponse>,
                            CompleteBookingHandler>();
builder.Services.AddScoped<IRequestHandler<CancelBookingRequest, CancelBookingResponse>,
                            CancelBookingHandler>();
builder.Services.AddScoped<IRequestHandler<ConfirmBookingRequest, ConfirmBookingResponse>,
                            ConfirmBookingHandler>();

// Chain of Responsibility: Already used via Mediator handlers

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped(sc => ShoppingCart.GetShoppingCart(sc));

// Authentication and authorization
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddMemoryCache();
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

builder.Services.AddSession();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Movies}/{action=Index}/{id?}");

// Seed Database
AppDbInitializer.Seed(app);
AppDbInitializer.SeedUsersAndRolesAsync(app).Wait();

app.Run();
