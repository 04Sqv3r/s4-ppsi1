using System.Collections.Generic;

namespace meow.Models
{
    // GŁÓWNY MODEL WIDOKU PROFILU
    public class ProfileViewModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public List<RentalHistoryItem> Rentals { get; set; } = new();
        public List<OrderGroupViewModel> Packages { get; set; } = new(); 
        
        // --- SEKCJA KAR I PŁATNOŚCI (Z PUNKTU 2) ---
        public List<FineItemViewModel> Fines { get; set; } = new();
        public decimal TotalFinesAmount { get; set; }
    }

    // POJEDYNCZA POZYCJA KARY/OPŁATY
    public class FineItemViewModel
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string DateGenerated { get; set; } = string.Empty;
    }

    // HISTORIA WYPOŻYCZEŃ BIBLIOTECZNYCH
    public class RentalHistoryItem
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string RentalDate { get; set; } = string.Empty;
        public string ReturnDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // GŁÓWNY KONTENER ZAMÓWIENIA INTERNETOWEGO
    public class OrderGroupViewModel
    {
        public int OrderId { get; set; }
        public string OrderDate { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; } // Sumaryczna wartość całego zamówienia
        public List<OrderItemViewModel> Items { get; set; } = new(); // Lista książek w tym zamówieniu
    }

    // POJEDYNCZA KSIĄŻKA WEWNĄTRZ ZAMÓWIENIA
    public class OrderItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
    }
}