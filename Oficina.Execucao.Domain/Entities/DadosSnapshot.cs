namespace Oficina.Execucao.Domain.Entities;

/// <summary>
/// Snapshot denormalizado do cliente e veículo, vindo do evento OSCriada.
/// Permite à fila exibir informações sem consultar o OS Service.
/// </summary>
public record DadosSnapshot(
    string ClienteNome,
    string ClienteCpf,
    string ClienteEmail,
    string ClienteTelefone,
    string VeiculoPlaca,
    string VeiculoMarca,
    string VeiculoModelo,
    int VeiculoAno);
