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




            CctBE100.CctBEDocumentoLiq CabLiq = DocumentoOrigem;
            var idHistorico = CabLiq.LinhasLiquidacao.GetEdita(1).IDHistorico;

            // TODO E SO PARA FORNECEDOR 




            var queryPendentes = $@"SELECT * fROM pendentes  WHERE idhistorico = '{idHistorico}'";
   
            var pendentesDados = BSO.Consulta(queryPendentes);
            var numPendentes = pendentesDados.NumLinhas();

            objBE.Linhas.RemoveTodos();

            pendentesDados.Inicio();
            for (int y = 1; y <= numPendentes; y++)
            {

                var modoPagamento = pendentesDados.Valor("ModoPag");
                var conta = pendentesDados.Valor("Conta");


                var queryLinhasHistorico = $@"SELECT * FROM LinhasPendentes WHEre idhistorico = '{idHistorico}'";
                var linhasDados = BSO.Consulta(queryLinhasHistorico);
                var numlinhas = linhasDados.NumLinhas();
                linhasDados.Inicio();
                for (int i = 1; i <= numlinhas ; i++)
                {
                    var linhasDescricao = linhasDados.DaValor<string>("Descricao");
                    double precUnit = linhasDados.DaValor<double>("Incidencia");
           

                    TesBELinhaDocTesouraria tesBELinhaDocTesouraria = new TesBELinhaDocTesouraria()
                    {



                        MovimentoBancario = modoPagamento.ToString(),
                 
                        Rubrica = linhasDescricao,

                        Debito = Math.Abs(precUnit),
                        DataMovimento = global::System.DateTime.Now,
                        Moeda = "EUR",
                        Conta = conta,
                        Arredondamento = 2,
                        CCustoCBL = "", // TODO JPV


                    };

                    objBE.Linhas.Insere(tesBELinhaDocTesouraria);



                    linhasDados.Seguinte();
                }
            }
        }
        public override void DepoisDeEditarLigacaoBancos(TesBEDocumentoTesouraria objBE, ExtensibilityEventArgs e)
        {
         

        }

 


    }
}
