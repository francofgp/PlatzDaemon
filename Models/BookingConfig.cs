namespace PlatzDaemon.Models;

public class BookingConfig
{
    /// <summary>Numero de WhatsApp del bot del club en formato internacional (sin +). Default: 5493534407576</summary>
    public string BotPhoneNumber { get; set; } = "5493534407576";

    /// <summary>Codigo de pais para normalizar numeros locales (ej: 54 = Argentina)</summary>
    public string CountryCode { get; set; } = "54";

    /// <summary>DNI / numero de documento del socio</summary>
    public string Dni { get; set; } = "";

    /// <summary>Hora de disparo en formato HH:mm (hora Argentina UTC-3). Default: 08:00</summary>
    public string TriggerTime { get; set; } = "08:00";

    /// <summary>Modo competitivo: pre-carga 20s antes y dispara exacto</summary>
    public bool CompetitiveMode { get; set; } = true;

    /// <summary>Periodo preferido: Mañana, Tarde, Noche</summary>
    public string PreferredPeriod { get; set; } = "Noche";

    /// <summary>Lista de horarios prioritarios en orden, formato HH:MMhs (ej: ["18:00hs", "19:00hs"])</summary>
    public List<string> PreferredTimeSlots { get; set; } = new() { "18:00hs", "19:00hs", "17:30hs" };

    /// <summary>Lista de canchas prioritarias en orden (ej: ["Cancha 6", "Cancha 8"])</summary>
    public List<string> PreferredCourts { get; set; } = new() { "Cancha Central", "Cancha 9" };

    /// <summary>Tipo de juego: Single o Doble</summary>
    public string GameType { get; set; } = "Doble";

    /// <summary>Dia de reserva: Hoy (unico soportado actualmente)</summary>
    public string BookingDay { get; set; } = "Hoy";

    /// <summary>Si la automatizacion esta habilitada para disparar</summary>
    public bool Enabled { get; set; } = true;
}
