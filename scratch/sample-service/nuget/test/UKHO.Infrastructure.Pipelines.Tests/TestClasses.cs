﻿using UKHO.Infrastructure.Pipelines.Contexts;
using UKHO.Infrastructure.Pipelines.Nodes;

namespace UKHO.Infrastructure.Pipelines.Tests
{
    public class SimpleTestNodeA1 : Node<TestObjectA>
    {
        private readonly bool _cancelProcessing;
        private readonly bool _shouldExecute = true;
        private readonly bool _shouldExecuteCancelProcessing;

        public SimpleTestNodeA1()
        {
        }

        public SimpleTestNodeA1(bool shouldExecute, bool shouldExecuteCancelProcessing = false, bool cancelProcessing = false)
        {
            _shouldExecute = shouldExecute;
            _shouldExecuteCancelProcessing = shouldExecuteCancelProcessing;
            _cancelProcessing = cancelProcessing;
        }

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<TestObjectA> context)
        {
            if (_shouldExecuteCancelProcessing)
            {
                context.CancelProcessing = true;
            }

            return Task.FromResult(_shouldExecute);
        }

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectA> context)
        {
            context.Subject.TestValueString = "Completed";

            return Task.FromResult(NodeResultStatus.Succeeded);
        }

        protected override void OnBeforeExecute(IExecutionContext<TestObjectA> context)
        {
            base.OnBeforeExecute(context);
            if (_cancelProcessing)
            {
                context.CancelProcessing = true;
            }
        }
    }

    public class SimpleTestNodeA2 : Node<TestObjectA>
    {
        private readonly bool _shouldExecute = true;

        public SimpleTestNodeA2()
        {
        }

        public SimpleTestNodeA2(bool shouldExecute) => _shouldExecute = shouldExecute;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<TestObjectA> context) => Task.FromResult(_shouldExecute);

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectA> context)
        {
            context.Subject.TestValueInt = 100;

            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }

    public class SubjectChangingNode1 : Node<TestObjectA>
    {
        private readonly bool _shouldExecute = true;

        public SubjectChangingNode1()
        {
        }

        public SubjectChangingNode1(bool shouldExecute) => _shouldExecute = shouldExecute;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<TestObjectA> context) => Task.FromResult(_shouldExecute);

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectA> context)
        {
            context.ChangeSubject(new TestObjectA { TestValueString = "New Instance" });

            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }

    public class FaultingTestNodeA : Node<TestObjectA>
    {
        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectA> context)
        {
            context.Subject.TestValueString = "Faulted";

            throw new Exception("Test Exception");
        }
    }

    public class FailingTestNodeA : Node<TestObjectA>
    {
        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectA> context)
        {
            context.Subject.TestValueString = "Failed";

            return Task.FromResult(NodeResultStatus.Failed);
        }
    }

    public class SimpleTestNodeASub1 : Node<TestObjectASub>
    {
        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectASub> context)
        {
            context.Subject.TestValueString = "Completed";

            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }


    public class SimpleTestNodeB1 : Node<TestObjectB>
    {
        private readonly bool _shouldExecute = true;

        public SimpleTestNodeB1()
        {
        }

        public SimpleTestNodeB1(bool shouldExecute) => _shouldExecute = shouldExecute;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<TestObjectB> context) => Task.FromResult(_shouldExecute);

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectB> context)
        {
            context.Subject.TestValueBool = true;

            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }

    public class SimpleTestNodeB2 : Node<TestObjectB>
    {
        private readonly bool _shouldExecute = true;

        public SimpleTestNodeB2()
        {
        }

        public SimpleTestNodeB2(bool shouldExecute) => _shouldExecute = shouldExecute;

        public override Task<bool> ShouldExecuteAsync(IExecutionContext<TestObjectB> context) => Task.FromResult(_shouldExecute);

        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectB> context)
        {
            context.Subject.TestValueBool = false;

            return Task.FromResult(NodeResultStatus.Succeeded);
        }
    }

    public class FaultingTestNodeB : Node<TestObjectB>
    {
        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectB> context)
        {
            context.Subject.TestValueBool = false;

            throw new Exception("Test Exception");
        }
    }

    public class FailingTestNodeB : Node<TestObjectB>
    {
        protected override Task<NodeResultStatus> PerformExecuteAsync(IExecutionContext<TestObjectB> context)
        {
            context.Subject.TestValueBool = false;

            return Task.FromResult(NodeResultStatus.Failed);
        }
    }

    public class ShouldExecuteBlockA : ShouldExecuteBlock<TestObjectA>
    {
    }

    public class ShouldNotExecuteBlockA : ShouldExecuteBlock<TestObjectA>
    {
        public override Task<bool> ShouldExecuteAsync(IExecutionContext<TestObjectA> context) => Task.FromResult(false);
    }

    public class TestObjectA
    {
        public string TestValueString { get; set; }

        public int TestValueInt { get; set; }
    }

    public class TestObjectASub : TestObjectA
    {
        public decimal TestValueDecimal;
    }

    public class TestObjectB
    {
        public bool TestValueBool { get; set; }
    }
}
