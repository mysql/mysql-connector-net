import java.io.*;
import org.antlr.runtime.*;
import org.antlr.runtime.debug.DebugEventSocketProxy;


public class __Test__ {

    public static void main(String args[]) throws Exception {
        MySQL51Lexer lex = new MySQL51Lexer(new ANTLRFileStream("C:\\src\\Parser\\M\\MySqlParser\\__Test___input.txt", "UTF8"));
        CommonTokenStream tokens = new CommonTokenStream(lex);

        MySQL51Parser g = new MySQL51Parser(tokens, 49101, null);
        try {
            g.statement_list();
        } catch (RecognitionException e) {
            e.printStackTrace();
        }
    }
}