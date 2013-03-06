using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Antlr.Runtime;

namespace MySqlParser
{	
	/// <summary>
	/// Abstract superclass for MySQL Lexers, for now containing some common code, so it's not in the grammar.
	/// Author: kroepke
	/// </summary>	
	public abstract class MySQLLexer : Lexer
	{
		bool nextTokenIsID = false;
    
		//private ErrorListener errorListener;

		/**
		 * Check ahead in the input stream for a left paren to distinguish between built-in functions
		 * and identifiers.
		 * TODO: This is the place to support certain SQL modes.
		 * @param proposedType the original token type for this input.
		 * @return the new token type to emit a token with
		 */
		public int checkFunctionAsID(int proposedType) {
			return (input.LA(1) != '(') ? MySQL51Lexer.ID : proposedType;
		}

		//public MySQLLexer() {} 

		public MySQLLexer(ICharStream input) : this(input, new RecognizerSharedState())
		{
			
			//errorListener = new BaseErrorListener(this);
		}

		public MySQLLexer(ICharStream input, RecognizerSharedState state)
			: base(input, state)
		{

			//errorListener = new BaseErrorListener(this);
		}

		/*
		public void setErrorListener(ErrorListener listener) {
			this.errorListener = listener;
		}

		public ErrorListener getErrorListener() {
			return errorListener;
		}

		// Delegate error presentation to our errorListener if that's set, otherwise pass up the chain.
		public void displayRecognitionError(String[] tokenNames, RecognitionException e) {
			errorListener.displayRecognitionError(tokenNames, e);
		}

		public String getErrorHeader(RecognitionException e) {
			return errorListener.getErrorHeader(e);
		}

		public void emitErrorMessage(String msg) {
			errorListener.emitErrorMessage(msg);
		}

		// generate more useful error messages for debugging 
		//@SuppressWarnings("unchecked")
		public String getErrorMessage(RecognitionException re, String[] tokenNames) {
    		 if (log.isLoggable(Level.FINEST)) {
    			 List stack = getRuleInvocationStack(re, this.getClass().getName());
    			 String msg;
    			 if (re instanceof NoViableAltException) {
    				 NoViableAltException nvae = (NoViableAltException)re;
    				 msg = "  no viable alternative for token="+ re.token + "\n" +
    				 "  at decision="+nvae.decisionNumber + " state=" + nvae.stateNumber;
    				 if (nvae.grammarDecisionDescription != null && nvae.grammarDecisionDescription.length() > 0)
    					 msg = msg + "\n  decision grammar=<< "+ nvae.grammarDecisionDescription + " >>";
    			 } else {
    				 msg = super.getErrorMessage(re, tokenNames);
    			 }
    			 return stack + "\n" + msg;
    		 } else {
    			 return errorListener.getErrorMessage(re, tokenNames);
    		 }
		 }

		 public void reportError(RecognitionException e) {
			 errorListener.reportError(e);
		 }

		// trampoline methods to get to super implementation 
		public void originalDisplayError(String[] tokenNames, RecognitionException e) {
			base.DisplayRecognitionError(tokenNames, e);
		}

		public void originalReportError(RecognitionException e) {
			base.ReportError(e);
		}

		public String originalGetErrorHeader(RecognitionException e) {
			return base.GetErrorHeader(e);
		}

		public String originalGetErrorMessage(RecognitionException e, String[] tokenNames) {
			return super.getErrorMessage(e, tokenNames);
		}

		public void originalEmitErrorMessage(String msg) {
			base.EmitErrorMessage(msg);
			log.warning(msg);
		}*/
	}
}



