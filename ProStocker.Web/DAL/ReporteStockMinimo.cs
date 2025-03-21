// StockDAL.cs
using ProStocker.Web.Models;

public class ReporteStockMinimo
{
    public Sucursal Sucursal { get;  set; }
    public Articulo Articulo { get;  set; }
    public decimal Stock { get;  set; }
    public decimal StockMinimo { get;  set; }
}