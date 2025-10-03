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
            // Documento de origem (liquida��o)
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

            // Pendentes do hist�rico
            var queryPendentes = $@"SELECT * FROM Pendentes WHERE IDHistorico = '{idHistorico}'";
            var pendentesDados = BSO.Consulta(queryPendentes);
            var numPendentes = pendentesDados.NumLinhas();

            if (numPendentes <= 0)
                return; // sem pendentes ? comportamento normal

            pendentesDados.Inicio();
            // Assumimos o 1� registo (ajusta se precisares de outra l�gica)
            var modoPagamento = pendentesDados.Valor("ModoPag") as string ?? "";
            //var conta = pendentesDados.Valor("Conta") as string ?? ""; // ResumoLiquidacoes
            


            // Linhas do hist�rico
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

            // 1) Verificar se existe pelo menos uma linha v�lida (Descricao n�o nula/vazia)
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

            // Se n�o houver linhas v�lidas, n�o tocamos no documento ? comportamento normal
            if (!temLinhasValidas)
                return;

            // 2) H� linhas v�lidas ? aplicar l�gica personalizada
            objBE.Linhas.RemoveTodos();

            linhasDados.Inicio();
            for (int i = 1; i <= numlinhas; i++)
            {
                var linhasDescricao = linhasDados.DaValor<string>("Descricao");
                if (string.IsNullOrWhiteSpace(linhasDescricao))
                {
                    linhasDados.Seguinte();
                    continue; // ignora linhas sem descri��o
                }

                // Total (incid�ncia com IVA, de acordo com o teu coment�rio original)
                double totalLinha = 0.0;
                try
                {
                    totalLinha = linhasDados.DaValor<double>("Total");
                }
                catch
                {
                    // Se der erro na convers�o, ignora esta linha
                    linhasDados.Seguinte();
                    continue;
                }

                var cCustocbl = linhasDados.Valor("CCustoCBL") as string ?? "";



                var tesBELinhaDocTesouraria = new TesBELinhaDocTesouraria()
                {
                    MovimentoBancario = modoPagamento,     // usa o modo de pagamento obtido dos pendentes
                    Rubrica = linhasDescricao,              // descri��o da linha
                    Debito = Math.Abs(totalLinha),         // valor a d�bito
                    DataMovimento = DateTime.Now,
                    Moeda = "EUR",
                    Arredondamento = 2,
                    CCustoCBL = cCustocbl,                 // centro de custo (se aplic�vel)
                    Conta = conta,
                };

                objBE.Linhas.Insere(tesBELinhaDocTesouraria);

                linhasDados.Seguinte();
            }
        }

        public override void DepoisDeEditarLigacaoBancos(TesBEDocumentoTesouraria objBE, ExtensibilityEventArgs e)
        {
            // Mant�m como est�, sem l�gica extra
        }
    }
}
