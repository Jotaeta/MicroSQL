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
                                DELETE FROM,DELETE FROM
                                WHERE,WHERE
                                CREATE TABLE,CREATE TABLE
                                DROP TABLE,DROP TABLE
                                INSERT INTO,INSERT INTO
                                VALUES,VALUES
                                UPDATE,UPDATE
                                SET,SET
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
                case "DELETE FROM":
                    return this.delete(arrayDeInstrucciones);
                case "DROP TABLE":
                    return this.dropTable(arrayDeInstrucciones);
                case "UPDATE":
                    return this.update(arrayDeInstrucciones);
                default:
                    throw new ErrorDeSemantica("NO SE RECONOCE EL COMANDO. Revise el archivo de configuracion");
            }            
        }

        //METODO PARCIAL
        private Resultado createTable(string[] arrayDeInstrucciones)
        {
            int i = 0;
            //NOMBRE DE LA TABLA
            string nombre = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.validarIdentificador(nombre);
            this.validarParentesis(this.instruccionByIndex(arrayDeInstrucciones, ++i));

            //DICCIONARIO PARA LAS COLUMNAS
            Dictionary<string, string> columnas = new Dictionary<string, string>();
            String llavePrimaria=null;

            //RECORRER TODAS LAS COLUMNAS A CREAR
            while(++i< arrayDeInstrucciones.Length && this.instruccionByIndex(arrayDeInstrucciones, i) != ")")
            {
                string instruccion = this.instruccionByIndex(arrayDeInstrucciones, i);
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
            this.validarParentesis(this.instruccionByIndex(arrayDeInstrucciones, i), true);

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
            string nombreTabla = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.validarIdentificador(nombreTabla);                        

            //OBTENER COLUMNAS DESDE ( HASTA )
            List<string> columnas = this.extraerColumnas(arrayDeInstrucciones, ref i, true);

            //----------- PARTE 2 ------------------
            //VALIDAR PALABRA VALUES
            this.palabrasReservadas.TryGetValue(this.instruccionByIndex(arrayDeInstrucciones, ++i), out instruccion);
            this.validarPalabraReservada("VALUES", instruccion);
            this.validarParentesis(this.instruccionByIndex(arrayDeInstrucciones, ++i));

            int indexColumna = 0;
            //RECORRER VALORES A INSERTAR
            while (++i < arrayDeInstrucciones.Length && this.instruccionByIndex(arrayDeInstrucciones, i) != ")")
            {                
                //REMOVER COMA AL FINAL                
                instruccion = this.removerComaAlFinal(this.instruccionByIndex(arrayDeInstrucciones, i));

                //VALOR
                string valor = instruccion.Trim().ToLower();
                this.validarValor(valor);
                data.Add(columnas.ElementAt(indexColumna++), valor);
            }

            //VALIDAR PARENTESIS DE CIERRE
            this.validarParentesis(this.instruccionByIndex(arrayDeInstrucciones, i), true);

            Tabla tabla = Tabla.cargarTabla(Tabla.rutaTabla(nombreTabla));
            string mensaje = tabla.insertar(data);
            tabla.guardar();
            return new Resultado("insert", mensaje);
        }

        private Resultado select(string[] arrayDeInstrucciones)
        {
            int i = 0;
            String instruccion = this.instruccionByIndex(arrayDeInstrucciones, ++i);

            //OBTENER COLUMNAS
            List<string> columnas = null;
            if (instruccion.Trim() != "*")
            {
                i--;
                columnas= this.extraerColumnas(arrayDeInstrucciones, ref i);
            }
            else
            {
                i++;
            }

            //VALIDAR PALABRA FROM
            this.palabrasReservadas.TryGetValue(this.instruccionByIndex(arrayDeInstrucciones, i), out instruccion);
            this.validarPalabraReservada("FROM", instruccion);

            //NOMBRE DE LA TABLA
            string nombreTabla = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.validarIdentificador(nombreTabla);

            string columnaFiltro = null, valorFiltro = null;

            if (arrayDeInstrucciones.Count() - 1 > i)
            {
                //VALIDAR PALABRA FROM
                this.palabrasReservadas.TryGetValue(this.instruccionByIndex(arrayDeInstrucciones, ++i), out instruccion);
                this.validarPalabraReservada("WHERE", instruccion);
                instruccion = this.instruccionByIndex(arrayDeInstrucciones, ++i);
                this.interpretarFiltro(instruccion, out columnaFiltro, out valorFiltro);                
            }

            Tabla tabla = Tabla.cargarTabla(Tabla.rutaTabla(nombreTabla));
            List<object> dataset = tabla.select(ref columnas, columnaFiltro, valorFiltro);

            return new Resultado("select", new object[2] { columnas, dataset });
        }

        private Resultado delete(string[] arrayDeInstrucciones)
        {
            int i = 0;
            String instruccion;

            //NOMBRE DE LA TABLA
            string nombreTabla = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.validarIdentificador(nombreTabla);

            string columnaFiltro = null, valorFiltro = null;

            if (arrayDeInstrucciones.Count() - 1 > i)
            {
                //VALIDAR PALABRA WHERE
                this.palabrasReservadas.TryGetValue(this.instruccionByIndex(arrayDeInstrucciones, ++i), out instruccion);
                this.validarPalabraReservada("WHERE", instruccion);
                instruccion = this.instruccionByIndex(arrayDeInstrucciones, ++i);
                this.interpretarFiltro(instruccion, out columnaFiltro, out valorFiltro);
            }

            Tabla tabla = Tabla.cargarTabla(Tabla.rutaTabla(nombreTabla));
            int registrosEliminados= tabla.delete(columnaFiltro, valorFiltro);
            tabla.guardar();
            return new Resultado("delete", "Se eliminaron " +registrosEliminados+ " de la tabla " + nombreTabla);
        }

        private Resultado dropTable(string[] arrayDeInstrucciones)
        {
            int i = 0;            

            //NOMBRE DE LA TABLA
            string nombreTabla = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.validarIdentificador(nombreTabla);

            string mensaje = "No se encontro la tabla a eliminar";
            if (Tabla.borrarTabla(Tabla.rutaTabla(nombreTabla)))
            {
                mensaje = "Tabla "+ nombreTabla+ " eliminada con exito";
            }
            else
            {
                throw new ErrorDeSemantica(mensaje);
            }

            return new Resultado("drop_table", new object[2] { mensaje, nombreTabla});
        }

        private Resultado update(string[] arrayDeInstrucciones)
        {
            int i = 0;
            String instruccion;
            string columnaActualizar=null, valorActualizar=null, columnaFiltro = null, valorFiltro = null;

            //NOMBRE DE LA TABLA
            string nombreTabla = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.validarIdentificador(nombreTabla);

            //VALIDAR PALABRA SET
            this.palabrasReservadas.TryGetValue(this.instruccionByIndex(arrayDeInstrucciones, ++i), out instruccion);
            this.validarPalabraReservada("SET", instruccion);

            //OBTENER VALOR Y COLUMNA A ACTUALIZAR
            instruccion = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.validarAsignacion(instruccion);
            columnaActualizar= instruccion.Split("=")[0].Trim();
            valorActualizar = instruccion.Split("=")[1].Trim();

            //VALIDAR PALABRA WHERE
            this.palabrasReservadas.TryGetValue(this.instruccionByIndex(arrayDeInstrucciones, ++i), out instruccion);
            this.validarPalabraReservada("WHERE", instruccion);
            instruccion = this.instruccionByIndex(arrayDeInstrucciones, ++i);
            this.interpretarFiltro(instruccion, out columnaFiltro, out valorFiltro);

            //MODIFICAR FILA EN LA TABLA
            Tabla tabla = Tabla.cargarTabla(Tabla.rutaTabla(nombreTabla));
            int resultado = tabla.update(columnaActualizar, valorActualizar, columnaFiltro, valorFiltro);
            tabla.guardar();
            return new Resultado("update", "Se actualizaron "+resultado + " registros");
        }

        private List<string> extraerColumnas(string[] arrayDeInstrucciones, ref int i, bool entreParentesis= false)
        {
            if(entreParentesis)
                this.validarParentesis(this.instruccionByIndex(arrayDeInstrucciones, ++i));            

            //LISTA TEMPORAL PARA LAS COLUMNAS
            List<string> columnas = new List<string>();
            String instruccion;

            //RECORRER TODAS LAS COLUMNAS A INSERTAR VALORES
            while (++i < arrayDeInstrucciones.Length 
                && this.instruccionByIndex(arrayDeInstrucciones, i) != ")"
                && !this.palabrasReservadas.ContainsKey(this.instruccionByIndex(arrayDeInstrucciones, i))
                )
            {
                instruccion = this.instruccionByIndex(arrayDeInstrucciones, i);
                //REMOVER COMA AL FINAL                
                instruccion = this.removerComaAlFinal(instruccion);

                //DATOS DE LA COLUMNA
                string nombreColumna = instruccion.ToLower();
                this.validarIdentificador(nombreColumna);
                columnas.Add(nombreColumna);
            }
            //VALIDAR PARENTESIS DE CIERRE
            if(entreParentesis)
                this.validarParentesis(this.instruccionByIndex(arrayDeInstrucciones, i), true);

            if (columnas.Count() < 1)
            {
                throw new ErrorDeSintaxis("Se esperaba al menos una columna");
            }
            return columnas;
        }

        private void interpretarFiltro(string linea, out string columnaFiltro, out string valorFiltro )
        {
            linea = linea.ToLower();
            this.validarFiltro(linea);

            if (linea.Contains("like"))
            {
                columnaFiltro = linea.Split("like")[0].Trim();
                valorFiltro = linea.Split("like")[1].Trim().Replace("%","(.*)");
            }
            else
            {
                columnaFiltro = linea.Split("=")[0].Trim();
                valorFiltro = linea.Split("=")[1].Trim();
            }
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

        private bool validarFiltro(string linea)
        {
            if (Regex.Match(linea, @"^[_a-zA-Z][_a-zA-Z0-9]*\s*(=|like)\s*(('[^']*')|(\d+))$").Success)
            {
                return true;
            }
            else
            {
                throw new ErrorDeSintaxis("Filtro where invalido: " + linea);
            }
        }

        private bool validarAsignacion(string linea)
        {
            if (Regex.Match(linea, @"^[_a-zA-Z][_a-zA-Z0-9]*\s*(=)\s*(('[^']*')|(\d+))$").Success)
            {
                return true;
            }
            else
            {
                throw new ErrorDeSintaxis("Asignacion invalida: " + linea);
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
                throw new ErrorDeSintaxis("Se esperaba: " + palabraCorrecta + " en lugar de " + palabraIngresada);
            }
            return true;
        }

        private string removerComaAlFinal(string linea)
        {
            if (linea.EndsWith(","))
                linea = linea.Substring(0, linea.Length - 1);
            return linea;
        }

        private string instruccionByIndex(string[] arrayDeInstrucciones, int i)
        {
            if(arrayDeInstrucciones.Count() > i)
            {
                return arrayDeInstrucciones[i].Trim();
            }
            else
            {
                throw new ErrorDeSintaxis("No se esperaba fin del archivo");
            }
        }
    }
}
