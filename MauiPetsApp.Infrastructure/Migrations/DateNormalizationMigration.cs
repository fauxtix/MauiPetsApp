using Dapper;
using System.Data;

namespace MauiPetsApp.Infrastructure.Migrations
{
    public static class DateNormalizationMigration
    {
        // call this once at startup (after backing up DB)
        public static void Run(IDbConnection connection)
        {
            connection.Open();
            using var tx = connection.BeginTransaction();
            try
            {
                // Vacina.DataToma
                connection.Execute(@"
                    UPDATE Vacina
                    SET DataToma = substr(DataToma,7,4) || '-' || substr(DataToma,4,2) || '-' || substr(DataToma,1,2)
                    WHERE DataToma LIKE '__/__/____';
                ", transaction: tx);

                // Racao.DataCompra
                connection.Execute(@"
                    UPDATE Racao
                    SET DataCompra = substr(DataCompra,7,4) || '-' || substr(DataCompra,4,2) || '-' || substr(DataCompra,1,2)
                    WHERE DataCompra LIKE '__/__/____';
                ", transaction: tx);

                // Desparasitante.DataAplicacao and DataProximaAplicacao
                connection.Execute(@"
                    UPDATE Desparasitante
                    SET DataAplicacao = substr(DataAplicacao,7,4) || '-' || substr(DataAplicacao,4,2) || '-' || substr(DataAplicacao,1,2)
                    WHERE DataAplicacao LIKE '__/__/____';
                ", transaction: tx);
                connection.Execute(@"
                    UPDATE Desparasitante
                    SET DataProximaAplicacao = substr(DataProximaAplicacao,7,4) || '-' || substr(DataProximaAplicacao,4,2) || '-' || substr(DataProximaAplicacao,1,2)
                    WHERE DataProximaAplicacao LIKE '__/__/____';
                ", transaction: tx);

                // ConsultaVeterinario.DataConsulta
                connection.Execute(@"
                    UPDATE ConsultaVeterinario
                    SET DataConsulta = substr(DataConsulta,7,4) || '-' || substr(DataConsulta,4,2) || '-' || substr(DataConsulta,1,2)
                    WHERE DataConsulta LIKE '__/__/____';
                ", transaction: tx);

                // Pet.DataNascimento and DataChip
                connection.Execute(@"
                    UPDATE Pet
                    SET DataNascimento = substr(DataNascimento,7,4) || '-' || substr(DataNascimento,4,2) || '-' || substr(DataNascimento,1,2)
                    WHERE DataNascimento LIKE '__/__/____';
                ", transaction: tx);
                connection.Execute(@"
                    UPDATE Pet
                    SET DataChip = substr(DataChip,7,4) || '-' || substr(DataChip,4,2) || '-' || substr(DataChip,1,2)
                    WHERE DataChip LIKE '__/__/____';
                ", transaction: tx);

                // Despesa.DataMovimento and DataCriacao
                connection.Execute(@"
                    UPDATE Despesa
                    SET DataMovimento = substr(DataMovimento,7,4) || '-' || substr(DataMovimento,4,2) || '-' || substr(DataMovimento,1,2)
                    WHERE DataMovimento LIKE '__/__/____';
                ", transaction: tx);
                connection.Execute(@"
                    UPDATE Despesa
                    SET DataCriacao = substr(DataCriacao,7,4) || '-' || substr(DataCriacao,4,2) || '-' || substr(DataCriacao,1,2)
                    WHERE DataCriacao LIKE '__/__/____';
                ", transaction: tx);

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }
    }
}
