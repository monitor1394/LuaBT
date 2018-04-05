local BTDecorator = bt.Class("BTDecorator",bt.BTNode)
bt.BTDecorator = BTDecorator

function BTDecorator:ctor()
    bt.BTNode.ctor(self)
    self.name = "BTDecorator"
    self.maxOutConnections = 1
end

function BTDecorator:getDecoratedConnection()
    return outConnection[1]
end

function BTDecorator:getDecoratedNode()
    return outConnection[1].targetNode
end
