using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using MicroSQL.Models;

namespace MicroSQL.Classes
{
    public class Interprete
    {
        public IDictionary<string, string> palabrasReservadas;
        const string rutaArchivoDeConfiguracion = "microSQL.ini";

        public Interprete()
        {
            this.palabrasReservadas = new Dictionary<string, string>();

            //SI NO EXISTE EL ARCHIVO CREAR CONFIGURACION DEFAULT
            if (!System.IO.File.Exists(rutaArchivoDeConfiguracion))
            {
                this.crearConfiguracionDefault();
            }

            //LECTURA DEL ARCHIVO microSQL.ini
            foreach (string linea in System.IO.File.ReadLines(rutaArchivoDeConfiguracion, Encoding.UTF8))
            {
                var array_linea = linea.Split(',');
                string llave = array_linea[1].Trim();
                string valor = array_linea[0].Trim();
                this.palabrasReservadas.Add(llave, valor);
            }

        }

        private string crearConfiguracionDefault()
        {
            string defaults = @"SELECT,SELECT
                                FROM,FROM
                                DELETE,DELETE
                                WHERE,WHERE
                                CREATE TABLE,CREATE TABLE
                                DROP TABLE,DROP TABLE
                                INSERT INTO,INSERT INTO
                                VALUES,VALUES
                                GO,GO";

            System.IO.StreamWriter streamWriter = new System.IO.StreamWriter(rutaArchivoDeConfiguracion);
            streamWriter.Write(defaults);
            streamWriter.Close();
            return defaults;
        }

        //METODO PRINCIPAL
        public object ejecutar(string texto)
        {
            //REMOVER LINEAS EN BLANCO            
            string[] arrayDeInstrucciones = Regex.Split( texto, @"\s*[\r\n]+\s*");
            string instruccion = "";

            //UTILIZANDO CONFIGURACION DE PALABRAS RESERVADAS
            this.palabrasReservadas.TryGetValue(arrayDeInstrucciones[0], out instruccion);

            switch (instruccion)
            {
                case "CREATE TABLE":
                   return this.createTable(arrayDeInstrucciones);
                default:
                    return instruccion;
            }            
        }

        //METODO PARCIAL
        private Tabla createTable(string[] arrayDeInstrucciones)
        {
            int i = 0;
            //NOMBRE DE LA TABLA
            string nombre = arrayDeInstrucciones[++i];
            this.validarIdentificador(nombre);
            this.validarParentesis(arrayDeInstrucciones[++i]);

            //DICCIONARIO PARA LAS COLUMNAS
            Dictionary<string, string> columnas = new Dictionary<string, string>();
            String llavePrimaria=null;

            //RECORRER TODAS LAS COLUMNAS A CREAR
            while(++i< arrayDeInstrucciones.Length && arrayDeInstrucciones[i].Trim() != ")")
            {
                string instruccion = arrayDeInstrucciones[i];
                //REMOVER COMA AL FINAL 
                if (instruccion.EndsWith(","))
                    instruccion= instruccion.Substring(0, instruccion.Length - 1);               

                //DATOS DE LA COLUMNA
                string nombreColumna = instruccion.Substring(0, instruccion.IndexOf(' ') + 1).Trim().ToLower();
                string tipo = instruccion.Substring(instruccion.IndexOf(' ') + 1).Trim().ToLower();
                this.validarIdentificador(nombreColumna);
                this.validarTipo(tipo);
                columnas.Add(nombreColumna, tipo);
                if (tipo.Contains("primary key"))
                    llavePrimaria = nombreColumna;

            }

            //VALIDAR PARENTESIS DE CIERRE
            this.validarParentesis(arrayDeInstrucciones[i], true);

            Tabla tabla =new Tabla(nombre, columnas, llavePrimaria);
            tabla.guardar();
            return tabla;
        }

        private bool validarParentesis(string linea, bool esCierre=false)
        {
            if(linea.Trim() == "(" && !esCierre || esCierre && linea.Trim()==")" )
            {
                return true;
            }
            else
            {
                throw new Exception("custom error");
            }            
        }

        private bool validarIdentificador(string linea)
        {
            if (Regex.Match(linea, @"[_a-zA-Z][_a-zA-Z0-9]*").Success)
            {
               return true;
            }
            else
            {
                throw new Exception("custom error");
            }
        }

        private bool validarTipo(string linea)
        {
            if (Regex.Match(linea, @"(varchar\([0-9]+\)|int|datetime)[primary key]?").Success)
            {
                return true;
            }
            else
            {
                throw new Exception("custom error: " + linea);
            }
        }
    }
}
