local BTDecorator = bt.Class("BTDecorator",bt.BTNode)
bt.BTDecorator = BTDecorator

function BTDecorator:ctor()
    bt.BTNode.ctor(self)
    self.name = "BTDecorator"
    self.maxOutConnections = 1
end

function BTDecorator:getDecoratedConnection()
    return self.outConnections[1]
end

function BTDecorator:getDecoratedNode()
    return self.outConnections[1].targetNode
end
