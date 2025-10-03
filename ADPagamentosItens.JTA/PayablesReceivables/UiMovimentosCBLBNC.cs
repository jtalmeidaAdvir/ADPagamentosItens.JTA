using Primavera.Extensibility.PayablesReceivables.Editors;
using Primavera.Extensibility.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Primavera.Extensibility.BusinessEntities.ExtensibilityService.EventArgs;
using TesBE100;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;

namespace ADPagamentosItens.JTA.PayablesReceivables
{
    public class UiMovimentosCBLBNC : MovimentosCBLBNC
    {
        public override void AntesDeEditarLigacaoBancos(TesBEDocumentoTesouraria objBE, ExtensibilityEventArgs e)
        {
            // Documento de origem (liquidação)
            var tipo = DocumentoOrigem.GetType();
     
            if (tipo.ToString() == "VndBE100.VndBEDocumentoVenda") {
            
            return;
            }


            CctBE100.CctBEDocumentoLiq CabLiq = DocumentoOrigem;
            if (CabLiq == null)
                return;
      
            var idHistorico = CabLiq.LinhasLiquidacao.GetEdita(1).IDHistorico;
            var tipoEntidade = CabLiq.TipoEntidade.ToString();

            // Apenas para fornecedor
            if (tipoEntidade != "F")
                return;

            // Pendentes do histórico
            var queryPendentes = $@"SELECT * FROM Pendentes WHERE IDHistorico = '{idHistorico}'";
            var pendentesDados = BSO.Consulta(queryPendentes);
            var numPendentes = pendentesDados.NumLinhas();

            if (numPendentes <= 0)
                return; // sem pendentes ? comportamento normal

            pendentesDados.Inicio();
            // Assumimos o 1º registo (ajusta se precisares de outra lógica)
            var modoPagamento = pendentesDados.Valor("ModoPag") as string ?? "";
            //var conta = pendentesDados.Valor("Conta") as string ?? ""; // ResumoLiquidacoes
            


            // Linhas do histórico
            var queryLinhasHistorico = $@"SELECT * FROM LinhasPendentes WHERE IDHistorico = '{idHistorico}'";
            var linhasDados = BSO.Consulta(queryLinhasHistorico);
            var numlinhas = linhasDados.NumLinhas();

            var queryparametrosgcp = @"SELECT ContaPagamento FROM ParametrosGCP";
            var parametrosgcp = BSO.Consulta(queryparametrosgcp);

            string conta = "";
            if (!parametrosgcp.Vazia())
            {
                conta = parametrosgcp.Valor("ContaPagamento") as string ?? "";
            }



            if (numlinhas <= 0)
                return; // sem linhas ? comportamento normal

            // 1) Verificar se existe pelo menos uma linha válida (Descricao não nula/vazia)
            bool temLinhasValidas = false;

            linhasDados.Inicio();
            for (int i = 1; i <= numlinhas; i++)
            {
                var descCheck = linhasDados.DaValor<string>("Descricao");
                if (!string.IsNullOrWhiteSpace(descCheck))
                {
                    temLinhasValidas = true;
                    break;
                }
                linhasDados.Seguinte();
            }

            // Se não houver linhas válidas, não tocamos no documento ? comportamento normal
            if (!temLinhasValidas)
                return;

            // 2) Há linhas válidas ? aplicar lógica personalizada
            objBE.Linhas.RemoveTodos();

            linhasDados.Inicio();
            for (int i = 1; i <= numlinhas; i++)
            {
                var linhasDescricao = linhasDados.DaValor<string>("Descricao");
                if (string.IsNullOrWhiteSpace(linhasDescricao))
                {
                    linhasDados.Seguinte();
                    continue; // ignora linhas sem descrição
                }

                // Total (incidência com IVA, de acordo com o teu comentário original)
                double totalLinha = 0.0;
                try
                {
                    totalLinha = linhasDados.DaValor<double>("Total");
                }
                catch
                {
                    // Se der erro na conversão, ignora esta linha
                    linhasDados.Seguinte();
                    continue;
                }

                var cCustocbl = linhasDados.Valor("CCustoCBL") as string ?? "";



                var tesBELinhaDocTesouraria = new TesBELinhaDocTesouraria()
                {
                    MovimentoBancario = modoPagamento,     // usa o modo de pagamento obtido dos pendentes
                    Rubrica = linhasDescricao,              // descrição da linha
                    Debito = Math.Abs(totalLinha),         // valor a débito
                    DataMovimento = DateTime.Now,
                    Moeda = "EUR",
                    Arredondamento = 2,
                    CCustoCBL = cCustocbl,                 // centro de custo (se aplicável)
                    Conta = conta,
                };

                objBE.Linhas.Insere(tesBELinhaDocTesouraria);

                linhasDados.Seguinte();
            }
        }

        public override void DepoisDeEditarLigacaoBancos(TesBEDocumentoTesouraria objBE, ExtensibilityEventArgs e)
        {
            // Mantém como está, sem lógica extra
        }
    }
}
