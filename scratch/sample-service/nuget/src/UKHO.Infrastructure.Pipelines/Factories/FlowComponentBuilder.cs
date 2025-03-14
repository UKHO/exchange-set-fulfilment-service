using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UKHO.Infrastructure.Pipelines.Utility;

namespace UKHO.Infrastructure.Pipelines.Factories
{
    /// <summary>
    ///     Allows the addition of flow components (nodes or subflows) to a parent flow or component.
    ///     Also underlies the FlowBuilder.
    /// </summary>
    /// <typeparam name="T">Type of the flow subject.</typeparam>
    public class FlowComponentBuilder<T> : IFlowComponentBuilder<T>, IFlowBuilder<T>
    {
        private readonly FlowComponent<T> _component;

        /// <summary>
        ///     Constructs a new FlowComponentBuilder.
        /// </summary>
        /// <param name="component">FlowComponent to build up.</param>
        public FlowComponentBuilder(FlowComponent<T> component) => _component = component;

        /// <summary>
        ///     Adds a root node to this flow.
        /// </summary>
        /// <typeparam name="TNode">Type of the node to add.</typeparam>
        /// <param name="name">Optional name of the node if needed to find in IOC container.</param>
        /// <param name="id">Id of the flow.  This can be used for identification in debugging.  Flows default to the flow name.</param>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> AddRoot<TNode>(string name = null, string id = null) where TNode : INode<T> => AddRoot(typeof(TNode), name, id);

        /// <summary>
        ///     Adds a child node to this flow.
        /// </summary>
        /// <param name="nodeType">Type of the node to add.</param>
        /// <param name="name">Optional name of the node if needed to find in IOC container.</param>
        /// <param name="id">Id of the flow.  This can be used for identification in debugging.  Flows default to the flow name.</param>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> AddRoot(Type nodeType, string name = null, string id = null)
        {
            if (!_component.IsFlow)
            {
                throw new InvalidOperationException("This method is only valid for flow components.");
            }

            if (!typeof(INode<T>).IsAssignableFrom(nodeType))
            {
                throw new ArgumentException("nodeType must be assignable to INode<T>.", nameof(nodeType));
            }

            FlowComponent<T> child = _component.AddChild(new FlowComponent<T> { Type = nodeType, Name = name, Id = string.IsNullOrEmpty(id) ? name : id });
            return new FlowComponentBuilder<T>(child);
        }

        /// <summary>
        ///     Adds a previously registered flow by name as a child of this node.
        /// </summary>
        /// <param name="name">The name of the flow to add.</param>
        /// <param name="id">Id of the flow. This can be used for identification in debugging. Defaults to the flow name.</param>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> AddFlow(string name, string id = null) => AddFlow<T>(name, id);

        /// <summary>
        ///     Adds a previously registered flow by name as a child of this node.
        /// </summary>
        /// <param name="name">The name of the flow to add.</param>
        /// <param name="id">Id of the flow. This can be used for identification in debugging. Defaults to the flow name.</param>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> AddFlow<TNode>(string name, string id = null)
        {
            Guard.AgainstNullOrEmptyArgument("name", name);

            if (!typeof(IMultiNode<T>).IsAssignableFrom(_component.Type))
            {
                throw new InvalidOperationException("In order to have children, nodeType must be assignable to IMultiNode<T>.");
            }

            _component.AddChild(new FlowComponent<T> { Type = typeof(TNode), Name = name, IsFlow = true, Id = string.IsNullOrEmpty(id) ? name : id });
            return this;
        }

        /// <summary>
        ///     Adds a child node to this flow.
        /// </summary>
        /// <typeparam name="TNode">Type of the node to add.</typeparam>
        /// <param name="name">Optional name of the node if needed to find in IOC container.</param>
        /// <param name="id">
        ///     Id of the node. This can be used for identification in debugging. Defaults to the node type with the
        ///     name if included.
        /// </param>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> AddChild<TNode>(string name = null, string id = null) where TNode : INode<T> => AddChild(typeof(TNode), name, id);

        /// <summary>
        ///     Adds a child node to this flow.
        /// </summary>
        /// <param name="nodeType">Type of the node to add.</param>
        /// <param name="name">Optional name of the node if needed to find in IOC container.</param>
        /// <param name="id">
        ///     Id of the node. This can be used for identification in debugging. Defaults to the node type with the
        ///     name if included.
        /// </param>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> AddChild(Type nodeType, string name = null, string id = null)
        {
            if (!typeof(INode<T>).IsAssignableFrom(nodeType))
            {
                throw new ArgumentException("nodeType must be assignable to INode<T>.", nameof(nodeType));
            }

            if (!typeof(IMultiNode<T>).IsAssignableFrom(_component.Type))
            {
                throw new InvalidOperationException("In order to have children, nodeType must be assignable to IMultiNode<T>.");
            }

            _component.AddChild(new FlowComponent<T> { Type = nodeType, Name = name, Id = id });
            return this;
        }


        /// <summary>
        ///     Adds a ShouldExecuteBlock to the flowcomponent (to be added to the resultant node).
        /// </summary>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> SetShouldExecuteBlock<TBlock>() where TBlock : IShouldExecuteBlock<T>
        {
            _component.SetShouldExecute(typeof(TBlock));
            return this;
        }

        /// <summary>
        ///     Adds a ShouldExecuteBlock to the flowcomponent (to be added to the resultant node).
        /// </summary>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> SetShouldExecuteBlock(Type blockType)
        {
            _component.SetShouldExecute(blockType);
            return this;
        }

        /// <summary>
        ///     Adds a ShouldExecuteAsync to the FlowComponent (to be added to the resultant node).
        /// </summary>
        /// <param name="shouldExecuteFunc">Function to add as ShouldExecute to the flowcomponent.</param>
        /// <returns>The current FlowComponentBuilder instance.</returns>
        public IFlowComponentBuilder<T> SetShouldExecute(Func<IExecutionContext<T>, Task<bool>> shouldExecuteFunc)
        {
            _component.SetShouldExecute(shouldExecuteFunc);
            return this;
        }

        /// <summary>
        ///     Allows metadata about the flow component to be added.
        /// </summary>
        /// <param name="key">Key of the data to add.</param>
        /// <param name="data">Data to add.</param>
        /// <returns></returns>
        public IFlowComponentBuilder<T> SetMetaData(string key, object data)
        {
            if (_component.MetaData.ContainsKey(key))
            {
                _component.MetaData[key] = data;
            }
            else
            {
                _component.MetaData.Add(key, data);
            }

            return this;
        }

        /// <summary>
        ///     Returns an instance of FlowComponentBuilder representing the requested child node.
        /// </summary>
        /// <typeparam name="TNode">Type of the node.</typeparam>
        /// <param name="name">Optional name of the node in IOC registration.</param>
        /// <param name="index">Index of the node if multiple matches are found in the parent.  Defaults to first.</param>
        /// <returns>A child FlowComponentBuilder of this FlowComponentBuilder.</returns>
        public IFlowComponentBuilder<T> ForChild<TNode>(string name = null, int index = 0) where TNode : INode<T> => ForChild(typeof(TNode), name, index);

        /// <summary>
        ///     Returns an instance of FlowComponentBuilder representing the requested child node.
        /// </summary>
        /// <param name="nodeType">Type of the node.</param>
        /// <param name="name">Optional name of the node in IOC registration.</param>
        /// <param name="index">Index of the node if multiple matches are found in the parent.  Defaults to first.</param>
        /// <returns>A child FlowComponentBuilder of this FlowComponentBuilder.</returns>
        public IFlowComponentBuilder<T> ForChild(Type nodeType, string name = null, int index = 0)
        {
            if (!typeof(INode<T>).IsAssignableFrom(nodeType))
            {
                throw new ArgumentException("nodeType must be assignable to INode<T>.", nameof(nodeType));
            }

            IEnumerable<FlowComponent<T>> items = _component.Children.Where(x => x.Type == nodeType);
            if (name != null)
            {
                items = items.Where(x => x.Name == name);
            }

            IList<FlowComponent<T>> results = items.ToList();

            if (index + 1 > results.Count)
            {
                throw new IndexOutOfRangeException("The requested child could not be found.");
            }

            FlowComponent<T> child = results[index];

            return new FlowComponentBuilder<T>(child);
        }

        /// <summary>
        ///     Returns an instance of FlowComponentBuilder representing the requested child flow.
        /// </summary>
        /// <param name="name">Optional name of the node in IOC registration.</param>
        /// <param name="index">Index of the node if multiple matches are found in the parent.  Defaults to first.</param>
        /// <returns>A child FlowComponentBuilder of this FlowComponentBuilder.</returns>
        public IFlowComponentBuilder<T> ForChildFlow(string name = null, int index = 0)
        {
            IEnumerable<FlowComponent<T>> items = _component.Children.Where(x => x.IsFlow);
            if (name != null)
            {
                items = items.Where(x => x.Name == name);
            }

            IList<FlowComponent<T>> results = items.ToList();

            if (index + 1 > results.Count)
            {
                throw new IndexOutOfRangeException("The requested child could not be found.");
            }

            FlowComponent<T> child = results[index];

            return new FlowComponentBuilder<T>(child);
        }

        /// <summary>
        ///     Returns an instance of FlowComponentBuilder representing the last child of the current builder.
        /// </summary>
        /// <returns>A child FlowComponentBuilder of this FlowComponentBuilder.</returns>
        public IFlowComponentBuilder<T> ForLastChild()
        {
            if (_component.Children == null || _component.Children.Count == 0)
            {
                throw new IndexOutOfRangeException("This item has no children.");
            }

            FlowComponent<T> child = _component.Children.Last();

            return new FlowComponentBuilder<T>(child);
        }

        /// <summary>
        ///     Returns an instance of FlowComponentBuilder representing the requested parent.
        /// </summary>
        /// <returns>A parent FlowComponentBuilder of this FlowComponentBuilder.</returns>
        public IFlowComponentBuilder<T> ForParent()
        {
            FlowComponent<T> parent = _component.Parent;

            if (parent == null)
            {
                throw new InvalidOperationException("This item has no parent.");
            }

            return new FlowComponentBuilder<T>(parent);
        }
    }
}
