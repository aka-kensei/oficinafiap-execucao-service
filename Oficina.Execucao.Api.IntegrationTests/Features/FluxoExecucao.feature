#language: pt-BR
Funcionalidade: Fluxo de Execução da Oficina
  Como mecânico da oficina
  Quero registrar o diagnóstico, propor orçamento, iniciar e finalizar a execução
  Para que o estado da OS seja atualizado e a Saga avance pelos eventos publicados

  Cenário: Iniciar diagnóstico publica evento DiagnosticoIniciado
    Dado que existe uma OS "ABC1234" na fila aguardando diagnóstico
    Quando o mecânico "Carlos" inicia o diagnóstico
    Então o evento "DiagnosticoIniciado" é publicado com o mecânico "Carlos"
    E a execução fica com status "EmDiagnostico"

  Cenário: Propor orçamento publica OrcamentoPropostoPelaOficina
    Dado que existe uma OS "DEF5678" em diagnóstico
    Quando o mecânico propõe um orçamento de 350.00 com 1 item
    Então o evento "OrcamentoPropostoPelaOficina" é publicado com valor 350.00
    E a execução fica com status "AguardandoOrcamento"

  Cenário: Finalizar execução publica ExecucaoFinalizada
    Dado que existe uma OS "GHI9012" em execução
    Quando o mecânico finaliza a execução
    Então o evento "ExecucaoFinalizada" é publicado no barramento
    E a execução fica com status "Finalizada"
