//------------------------------------------------------------------------------ 
// <copyright file="DataBindingExpressionBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Compilation { 
    using System; 
    using System.Security.Permissions;
    using System.CodeDom; 
    using System.Diagnostics;
    using System.Reflection;
    using System.Web.UI;
 

    internal class DataBindingExpressionBuilder : ExpressionBuilder { 
        private static EventInfo eventInfo; 
        private const string EvalMethodName = "Eval";
        private const string GetDataItemMethodName = "GetDataItem"; 

        internal static EventInfo Event {
            get {
                if (eventInfo == null) { 
                    eventInfo = typeof(Control).GetEvent("DataBinding");
                } 
 
                return eventInfo;
            } 
        }

        internal static void BuildEvalExpression(string field, string formatString, string propertyName,
            Type propertyType, ControlBuilder controlBuilder, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) { 

            // Altogether, this function will create a statement that looks like this: 
            // if (this.Page.GetDataItem() != null) { 
            //     target.{{propName}} = ({{propType}}) this.Eval(fieldName, formatString);
            // } 

            //     this.Eval(fieldName, formatString)
            CodeMethodInvokeExpression evalExpr = new CodeMethodInvokeExpression();
            evalExpr.Method.TargetObject = new CodeThisReferenceExpression(); 
            evalExpr.Method.MethodName = EvalMethodName;
            evalExpr.Parameters.Add(new CodePrimitiveExpression(field)); 
            if (!String.IsNullOrEmpty(formatString)) { 
                evalExpr.Parameters.Add(new CodePrimitiveExpression(formatString));
            } 

            CodeStatementCollection evalStatements = new CodeStatementCollection();
            BuildPropertySetExpression(evalExpr, propertyName, propertyType, controlBuilder, methodStatements, evalStatements, linePragma, ref hasTempObject);
 
            // if (this.Page.GetDataItem() != null)
            CodeMethodInvokeExpression getDataItemExpr = new CodeMethodInvokeExpression(); 
            getDataItemExpr.Method.TargetObject = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Page"); 
            getDataItemExpr.Method.MethodName = GetDataItemMethodName;
 
            CodeConditionStatement ifStmt = new CodeConditionStatement();
            ifStmt.Condition = new CodeBinaryOperatorExpression(getDataItemExpr,
                                                                CodeBinaryOperatorType.IdentityInequality,
                                                                new CodePrimitiveExpression(null)); 
            ifStmt.TrueStatements.AddRange(evalStatements);
            statements.Add(ifStmt); 
        } 

        private static void BuildPropertySetExpression(CodeExpression expression, string propertyName, 
            Type propertyType, ControlBuilder controlBuilder, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) {

            CodeDomUtility.CreatePropertySetStatements(methodStatements, statements,
                new CodeVariableReferenceExpression("dataBindingExpressionBuilderTarget"), propertyName, propertyType, 
                expression,
                linePragma); 
        } 

        internal static void BuildExpressionSetup(ControlBuilder controlBuilder, CodeStatementCollection methodStatements, CodeStatementCollection statements) { 
            // {{controlType}} target;
            CodeVariableDeclarationStatement targetDecl = new CodeVariableDeclarationStatement(controlBuilder.ControlType, "dataBindingExpressionBuilderTarget");
            methodStatements.Add(targetDecl);
 
            CodeVariableReferenceExpression targetExp = new CodeVariableReferenceExpression(targetDecl.Name);
 
            // target = ({{controlType}}) sender; 
            CodeAssignStatement setTarget = new CodeAssignStatement(targetExp,
                                                                    new CodeCastExpression(controlBuilder.ControlType, 
                                                                                           new CodeArgumentReferenceExpression("sender")));
            statements.Add(setTarget);

            Type bindingContainerType = controlBuilder.BindingContainerType; 
            CodeVariableDeclarationStatement containerDecl = new CodeVariableDeclarationStatement(bindingContainerType, "Container");
            methodStatements.Add(containerDecl); 
 
            // {{containerType}} Container = ({{containerType}}) target.BindingContainer;
            CodeAssignStatement setContainer = new CodeAssignStatement(new CodeVariableReferenceExpression(containerDecl.Name), 
                                                                       new CodeCastExpression(bindingContainerType,
                                                                                              new CodePropertyReferenceExpression(targetExp,
                                                                                                                                  "BindingContainer")));
            statements.Add(setContainer); 
        }
 
        internal override void BuildExpression(BoundPropertyEntry bpe, ControlBuilder controlBuilder, 
            CodeExpression controlReference, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) {
 
            BuildExpressionStatic(bpe, controlBuilder, controlReference, methodStatements, statements, linePragma, ref hasTempObject);
        }

        internal static void BuildExpressionStatic(BoundPropertyEntry bpe, ControlBuilder controlBuilder, 
            CodeExpression controlReference, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) {
 
            CodeExpression expr = new CodeSnippetExpression(bpe.Expression); 
            BuildPropertySetExpression(expr, bpe.Name, bpe.Type, controlBuilder, methodStatements, statements, linePragma, ref hasTempObject);
        } 


        public override CodeExpression GetCodeExpression(BoundPropertyEntry entry,
            object parsedData, ExpressionBuilderContext context) { 
            Debug.Fail("This should never be called");
            return null; 
        } 
    }
} 
//------------------------------------------------------------------------------ 
// <copyright file="DataBindingExpressionBuilder.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.Compilation { 
    using System; 
    using System.Security.Permissions;
    using System.CodeDom; 
    using System.Diagnostics;
    using System.Reflection;
    using System.Web.UI;
 

    internal class DataBindingExpressionBuilder : ExpressionBuilder { 
        private static EventInfo eventInfo; 
        private const string EvalMethodName = "Eval";
        private const string GetDataItemMethodName = "GetDataItem"; 

        internal static EventInfo Event {
            get {
                if (eventInfo == null) { 
                    eventInfo = typeof(Control).GetEvent("DataBinding");
                } 
 
                return eventInfo;
            } 
        }

        internal static void BuildEvalExpression(string field, string formatString, string propertyName,
            Type propertyType, ControlBuilder controlBuilder, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) { 

            // Altogether, this function will create a statement that looks like this: 
            // if (this.Page.GetDataItem() != null) { 
            //     target.{{propName}} = ({{propType}}) this.Eval(fieldName, formatString);
            // } 

            //     this.Eval(fieldName, formatString)
            CodeMethodInvokeExpression evalExpr = new CodeMethodInvokeExpression();
            evalExpr.Method.TargetObject = new CodeThisReferenceExpression(); 
            evalExpr.Method.MethodName = EvalMethodName;
            evalExpr.Parameters.Add(new CodePrimitiveExpression(field)); 
            if (!String.IsNullOrEmpty(formatString)) { 
                evalExpr.Parameters.Add(new CodePrimitiveExpression(formatString));
            } 

            CodeStatementCollection evalStatements = new CodeStatementCollection();
            BuildPropertySetExpression(evalExpr, propertyName, propertyType, controlBuilder, methodStatements, evalStatements, linePragma, ref hasTempObject);
 
            // if (this.Page.GetDataItem() != null)
            CodeMethodInvokeExpression getDataItemExpr = new CodeMethodInvokeExpression(); 
            getDataItemExpr.Method.TargetObject = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Page"); 
            getDataItemExpr.Method.MethodName = GetDataItemMethodName;
 
            CodeConditionStatement ifStmt = new CodeConditionStatement();
            ifStmt.Condition = new CodeBinaryOperatorExpression(getDataItemExpr,
                                                                CodeBinaryOperatorType.IdentityInequality,
                                                                new CodePrimitiveExpression(null)); 
            ifStmt.TrueStatements.AddRange(evalStatements);
            statements.Add(ifStmt); 
        } 

        private static void BuildPropertySetExpression(CodeExpression expression, string propertyName, 
            Type propertyType, ControlBuilder controlBuilder, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) {

            CodeDomUtility.CreatePropertySetStatements(methodStatements, statements,
                new CodeVariableReferenceExpression("dataBindingExpressionBuilderTarget"), propertyName, propertyType, 
                expression,
                linePragma); 
        } 

        internal static void BuildExpressionSetup(ControlBuilder controlBuilder, CodeStatementCollection methodStatements, CodeStatementCollection statements) { 
            // {{controlType}} target;
            CodeVariableDeclarationStatement targetDecl = new CodeVariableDeclarationStatement(controlBuilder.ControlType, "dataBindingExpressionBuilderTarget");
            methodStatements.Add(targetDecl);
 
            CodeVariableReferenceExpression targetExp = new CodeVariableReferenceExpression(targetDecl.Name);
 
            // target = ({{controlType}}) sender; 
            CodeAssignStatement setTarget = new CodeAssignStatement(targetExp,
                                                                    new CodeCastExpression(controlBuilder.ControlType, 
                                                                                           new CodeArgumentReferenceExpression("sender")));
            statements.Add(setTarget);

            Type bindingContainerType = controlBuilder.BindingContainerType; 
            CodeVariableDeclarationStatement containerDecl = new CodeVariableDeclarationStatement(bindingContainerType, "Container");
            methodStatements.Add(containerDecl); 
 
            // {{containerType}} Container = ({{containerType}}) target.BindingContainer;
            CodeAssignStatement setContainer = new CodeAssignStatement(new CodeVariableReferenceExpression(containerDecl.Name), 
                                                                       new CodeCastExpression(bindingContainerType,
                                                                                              new CodePropertyReferenceExpression(targetExp,
                                                                                                                                  "BindingContainer")));
            statements.Add(setContainer); 
        }
 
        internal override void BuildExpression(BoundPropertyEntry bpe, ControlBuilder controlBuilder, 
            CodeExpression controlReference, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) {
 
            BuildExpressionStatic(bpe, controlBuilder, controlReference, methodStatements, statements, linePragma, ref hasTempObject);
        }

        internal static void BuildExpressionStatic(BoundPropertyEntry bpe, ControlBuilder controlBuilder, 
            CodeExpression controlReference, CodeStatementCollection methodStatements, CodeStatementCollection statements, CodeLinePragma linePragma, ref bool hasTempObject) {
 
            CodeExpression expr = new CodeSnippetExpression(bpe.Expression); 
            BuildPropertySetExpression(expr, bpe.Name, bpe.Type, controlBuilder, methodStatements, statements, linePragma, ref hasTempObject);
        } 


        public override CodeExpression GetCodeExpression(BoundPropertyEntry entry,
            object parsedData, ExpressionBuilderContext context) { 
            Debug.Fail("This should never be called");
            return null; 
        } 
    }
} 
