using Makoto.Api.Models;

namespace Makoto.Api.Contracts;

// ── Lo que ve el Administrador en el Dashboard ──

public sealed record CondicionResponse(
    int Id, string Codigo, GeneroVocal Genero, NivelFrecuencia Nivel,
    decimal F0ObjetivoHz, decimal? F0MedidoHz, bool Certificada);

public sealed record CuotaResponse(
    string Codigo, GeneroVocal Genero, NivelFrecuencia Nivel, int Completadas, int EnCurso);
