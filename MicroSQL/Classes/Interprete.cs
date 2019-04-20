using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using MicroSQL.Models;
using MicroSQL.Excepciones;

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
        public Resultado ejecutar(string texto)
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
                case "INSERT INTO":
                    return this.insert(arrayDeInstrucciones);
                case "SELECT":
                    return this.select(arrayDeInstrucciones);
                default:
                    return new Resultado(instruccion, null);
            }            
        }

        //METODO PARCIAL
        private Resultado createTable(string[] arrayDeInstrucciones)
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
                instruccion = this.removerComaAlFinal(instruccion);

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
            return new Resultado("create_table", tabla);
        }

        //INSERT 
        public Resultado insert(string[] arrayDeInstrucciones)
        {
            int i = 0;
            Dictionary<string, string> data = new Dictionary<string, string>();
            String instruccion;

            //NOMBRE DE LA TABLA
            string nombreTabla = arrayDeInstrucciones[++i];
            this.validarIdentificador(nombreTabla);                        

            //OBTENER COLUMNAS DESDE ( HASTA )
            List<string> columnas = this.extraerColumnas(arrayDeInstrucciones, ref i, true);

            //----------- PARTE 2 ------------------
            //VALIDAR PALABRA VALUES
            this.palabrasReservadas.TryGetValue(arrayDeInstrucciones[++i], out instruccion);
            this.validarPalabraReservada("VALUES", instruccion);
            this.validarParentesis(arrayDeInstrucciones[++i]);

            int indexColumna = 0;
            //RECORRER VALORES A INSERTAR
            while (++i < arrayDeInstrucciones.Length && arrayDeInstrucciones[i].Trim() != ")")
            {                
                //REMOVER COMA AL FINAL                
                instruccion = this.removerComaAlFinal(arrayDeInstrucciones[i]);

                //VALOR
                string valor = instruccion.Trim().ToLower();
                this.validarValor(valor);
                data.Add(columnas.ElementAt(indexColumna++), valor);
            }

            //VALIDAR PARENTESIS DE CIERRE
            this.validarParentesis(arrayDeInstrucciones[i], true);

            Tabla tabla = Tabla.cargarTabla(Tabla.rutaTabla(nombreTabla));
            string mensaje = tabla.insertar(data);
            tabla.guardar();
            return new Resultado("insert", mensaje);
        }

        private Resultado select(string[] arrayDeInstrucciones)
        {
            int i = 0;
            String instruccion;

            //OBTENER COLUMNAS DESDE HASTA 
            List<string> columnas = arrayDeInstrucciones[i].Trim() == "*" ? null : this.extraerColumnas(arrayDeInstrucciones, ref i);

            //VALIDAR PALABRA FROM
            this.palabrasReservadas.TryGetValue(arrayDeInstrucciones[i], out instruccion);
            this.validarPalabraReservada("FROM", instruccion);

            //NOMBRE DE LA TABLA
            string nombreTabla = arrayDeInstrucciones[++i];
            this.validarIdentificador(nombreTabla);

            string columnaFiltro = null, valorFiltro = null;

            if (arrayDeInstrucciones.Count() - 1 > i)
            {
                //VALIDAR PALABRA FROM
                this.palabrasReservadas.TryGetValue(arrayDeInstrucciones[++i], out instruccion);
                this.validarPalabraReservada("WHERE", instruccion);
                instruccion = arrayDeInstrucciones[++i];
                columnaFiltro = instruccion.Split("=")[0].Trim().ToLower();
                valorFiltro = instruccion.Split("=")[1].Trim().ToLower();
            }

            Tabla tabla = Tabla.cargarTabla(Tabla.rutaTabla(nombreTabla));
            List<object> dataset = tabla.select(ref columnas, columnaFiltro, valorFiltro);

            return new Resultado("select", new object[2] { columnas, dataset });
        }

        private List<string> extraerColumnas(string[] arrayDeInstrucciones, ref int i, bool entreParentesis= false)
        {
            if(entreParentesis)
                this.validarParentesis(arrayDeInstrucciones[++i]);            

            //LISTA TEMPORAL PARA LAS COLUMNAS
            List<string> columnas = new List<string>();
            String instruccion;

            //RECORRER TODAS LAS COLUMNAS A INSERTAR VALORES
            while (++i < arrayDeInstrucciones.Length 
                && arrayDeInstrucciones[i].Trim() != ")"
                && !this.palabrasReservadas.ContainsKey(arrayDeInstrucciones[i].Trim())
                )
            {
                instruccion = arrayDeInstrucciones[i];
                //REMOVER COMA AL FINAL                
                instruccion = this.removerComaAlFinal(instruccion);

                //DATOS DE LA COLUMNA
                string nombreColumna = instruccion.Trim().ToLower();
                this.validarIdentificador(nombreColumna);
                columnas.Add(nombreColumna);
            }
            //VALIDAR PARENTESIS DE CIERRE
            if(entreParentesis)
                this.validarParentesis(arrayDeInstrucciones[i], true);
            return columnas;
        }

        private bool validarParentesis(string linea, bool esCierre=false)
        {
            if(linea.Trim() == "(" && !esCierre || esCierre && linea.Trim()==")" )
            {
                return true;
            }
            else
            {
                throw new ErrorDeSintaxis("Se esperaba cierre de parentesis");
            }            
        }

        private bool validarValor(string linea)
        {
            if (Regex.Match(linea, @"^(('[^']*')|(^\d+))$").Success)
            {
                return true;
            }
            else
            {
                throw new ErrorDeSintaxis("El valor ["+ linea +"] no cumple con los formatos permitidos");
            }
        }

        private bool validarIdentificador(string linea)
        {
            if (Regex.Match(linea, @"^[_a-zA-Z][_a-zA-Z0-9]*$").Success)
            {
               return true;
            }
            else
            {
                throw new ErrorDeSintaxis("El identificador no cumple con las reglas definidas "+ linea);
            }
        }

        private bool validarTipo(string linea)
        {
            if (Regex.Match(linea, @"^(varchar\([0-9]+\)|int|datetime)[primary key]?$").Success)
            {
                return true;
            }
            else
            {
                throw new ErrorDeSintaxis("tipo invalido: " + linea);
            }
        }

        private bool validarPalabraReservada(string palabraCorrecta, string palabraIngresada)
        {
            if (palabraCorrecta != palabraIngresada)
            {
                throw new ErrorDeSintaxis("Se esperaba" + palabraCorrecta);
            }
            return true;
        }

        private string removerComaAlFinal(string linea)
        {
            if (linea.EndsWith(","))
                linea = linea.Substring(0, linea.Length - 1);
            return linea;
        }
    }
}
