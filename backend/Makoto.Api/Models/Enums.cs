namespace Makoto.Api.Models;

public enum GeneroVocal { Femenino, Masculino }

public enum NivelFrecuencia { Grave, Neutra, Aguda }

/// <summary>Ciclo de vida de una sesión: Iniciada → EnAudio → Completada. Expirada si se abandona.</summary>
public enum EstadoSesion { Iniciada, EnAudio, Completada, Expirada }

public enum TipoEvento { AudioStart, AudioEnd, Replay }
