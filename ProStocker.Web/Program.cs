using System.Data.SQLite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using ProStocker.Web.DAL;

var builder = WebApplication.CreateBuilder(args);

// Agregar servicios MVC
builder.Services.AddControllersWithViews();

// Agregar soporte para sesiones
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configurar autenticación con cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Registrar DAL
builder.Services.AddSingleton<DataAccess>();

var app = builder.Build();

// Inicializar la base de datos SQLite
string connectionString = builder.Configuration.GetConnectionString("SQLiteConnection");
string dbPath = Path.Combine(Directory.GetCurrentDirectory(), "ProStocker.db");

// Crear la base de datos y las tablas si no existen
if (!File.Exists(dbPath))
{
    SQLiteConnection.CreateFile(dbPath); // Crea el archivo de la base de datos
    using var conn = new SQLiteConnection(connectionString);
    conn.Open();

    // Ejecutar el script SQL para crear las tablas
    string sqlScript = @"
        CREATE TABLE Sucursales (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Direccion TEXT
        );
        CREATE TABLE Cajas (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SucursalId INTEGER NOT NULL,
            Nombre TEXT NOT NULL,
            Estado TEXT CHECK(Estado IN ('Activa', 'Inactiva')) DEFAULT 'Activa',
            FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id)
        );
        CREATE TABLE Usuarios (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Usuario TEXT UNIQUE NOT NULL,
            Contrasena TEXT NOT NULL,
            Tipo TEXT CHECK(Tipo IN ('Admin', 'Vendedor')) DEFAULT 'Vendedor'
        );
        CREATE TABLE UsuarioSucursal (
            UsuarioId INTEGER NOT NULL,
            SucursalId INTEGER NOT NULL,
            PRIMARY KEY (UsuarioId, SucursalId),
            FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id),
            FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id)
        );
        CREATE TABLE TurnosCaja (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CajaId INTEGER NOT NULL,
            UsuarioId INTEGER NOT NULL,
            FechaInicio TEXT NOT NULL,
            FechaFin TEXT,
            MontoInicial REAL NOT NULL,
            MontoFinal REAL,
            MontoReal REAL,
            Diferencia REAL,
            Estado TEXT CHECK(Estado IN ('Abierto', 'Cerrado')) DEFAULT 'Abierto',
            FOREIGN KEY (CajaId) REFERENCES Cajas(Id),
            FOREIGN KEY (UsuarioId) REFERENCES Usuarios(Id)
        );
        CREATE TABLE CajaMovimientos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TurnoId INTEGER NOT NULL,
            Tipo TEXT CHECK(Tipo IN ('Apertura', 'Cierre', 'Ajuste')) NOT NULL,
            FechaHora TEXT NOT NULL,
            Monto REAL NOT NULL,
            Notas TEXT,
            FOREIGN KEY (TurnoId) REFERENCES TurnosCaja(Id)
        );
        CREATE TABLE Articulos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Codigo TEXT UNIQUE NOT NULL,
            Descripcion TEXT NOT NULL,
            Imagen TEXT,
            Precio1 REAL NOT NULL,
            Precio2 REAL,
            Precio3 REAL,
            Costo REAL NOT NULL
        );
        CREATE TABLE StockPorSucursal (
            SucursalId INTEGER NOT NULL,
            ArticuloId INTEGER NOT NULL,
            Stock REAL NOT NULL DEFAULT 0,
            StockMinimo REAL NOT NULL DEFAULT 0,
            PRIMARY KEY (SucursalId, ArticuloId),
            FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
            FOREIGN KEY (ArticuloId) REFERENCES Articulos(Id)
        );
        CREATE TABLE Clientes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Nombre TEXT NOT NULL,
            Documento TEXT,
            Direccion TEXT,
            Telefono TEXT
        );
        CREATE TABLE Ventas (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SucursalId INTEGER NOT NULL,
            CajaId INTEGER NOT NULL,
            TurnoId INTEGER NOT NULL,
            Fecha TEXT NOT NULL,
            ClienteId INTEGER,
            VendedorId INTEGER NOT NULL,
            TotalCosto REAL NOT NULL,
            TotalVenta REAL NOT NULL,
            Estado TEXT CHECK(Estado IN ('Pendiente', 'Completada', 'Anulada')) DEFAULT 'Pendiente',
            FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
            FOREIGN KEY (CajaId) REFERENCES Cajas(Id),
            FOREIGN KEY (TurnoId) REFERENCES TurnosCaja(Id),
            FOREIGN KEY (ClienteId) REFERENCES Clientes(Id),
            FOREIGN KEY (VendedorId) REFERENCES Usuarios(Id)
        );
        CREATE TABLE VentaPagos (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            VentaId INTEGER NOT NULL,
            MetodoPago TEXT CHECK(MetodoPago IN ('Efectivo', 'Tarjeta', 'MercadoPago', 'Otro')) NOT NULL,
            Monto REAL NOT NULL,
            Detalle TEXT,
            Estado TEXT CHECK(Estado IN ('Pendiente', 'Confirmado')) DEFAULT 'Pendiente',
            FOREIGN KEY (VentaId) REFERENCES Ventas(Id)
        );
        CREATE TABLE Compras (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SucursalId INTEGER NOT NULL,
            Fecha TEXT NOT NULL,
            ProveedorId INTEGER,
            TotalCosto REAL NOT NULL,
            FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id)
        );
        CREATE TABLE MovimientosDinero (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            SucursalId INTEGER NOT NULL,
            CajaId INTEGER,
            TurnoId INTEGER,
            FechaHora TEXT NOT NULL,
            Tipo TEXT CHECK(Tipo IN ('Ingreso', 'Egreso')) NOT NULL,
            Monto REAL NOT NULL,
            Descripcion TEXT,
            FOREIGN KEY (SucursalId) REFERENCES Sucursales(Id),
            FOREIGN KEY (CajaId) REFERENCES Cajas(Id),
            FOREIGN KEY (TurnoId) REFERENCES TurnosCaja(Id)
        );
        
        -- Datos iniciales para pruebas
        INSERT INTO Sucursales (Nombre, Direccion) VALUES ('Sucursal 1', 'Calle 123');
        INSERT INTO Sucursales (Nombre, Direccion) VALUES ('Sucursal 2', 'Avenida 456');
        INSERT INTO Cajas (SucursalId, Nombre, Estado) VALUES (1, 'Caja 1', 'Activa');
        INSERT INTO Cajas (SucursalId, Nombre, Estado) VALUES (1, 'Caja 2', 'Activa');
        INSERT INTO Cajas (SucursalId, Nombre, Estado) VALUES (2, 'Caja 1', 'Activa');
        INSERT INTO Cajas (SucursalId, Nombre, Estado) VALUES (2, 'Caja 2', 'Activa');
        INSERT INTO Usuarios (Nombre, Usuario, Contrasena, Tipo) 
        VALUES ('Admin', 'admin', '$2a$11$hFiRf4p9TK5NG/QA4NHomuWfziydZJ8zH2CSRgooFaUlfCodlfYuy', 'Admin'); -- Contraseña: 'admin123'
        INSERT INTO UsuarioSucursal (UsuarioId, SucursalId) VALUES (1, 1);
        INSERT INTO UsuarioSucursal (UsuarioId, SucursalId) VALUES (1, 2);
        INSERT INTO Articulos (Codigo, Descripcion, Precio1, Costo) 
        VALUES ('123456789', 'Producto 1', 100.00, 80.00);
        INSERT INTO StockPorSucursal (SucursalId, ArticuloId, Stock, StockMinimo) 
        VALUES (1, 1, 50, 10);
        INSERT INTO StockPorSucursal (SucursalId, ArticuloId, Stock, StockMinimo) 
        VALUES (2, 1, 30, 10);

        INSERT INTO Usuarios (Nombre, Usuario, Contrasena, Tipo) VALUES ('Vendedor1', 'vendedor1', '$2a$11$hFiRf4p9TK5NG/QA4NHomuWfziydZJ8zH2CSRgooFaUlfCodlfYuy', 'Vendedor'); 
        INSERT INTO UsuarioSucursal (UsuarioId, SucursalId) VALUES (2, 1);


 
    ";

    using var cmd = new SQLiteCommand(sqlScript, conn);
    cmd.ExecuteNonQuery();
    conn.Close();
}

// Configurar pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Añadir autenticación
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();