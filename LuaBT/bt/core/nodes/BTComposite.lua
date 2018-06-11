
local BTComposite = bt.Class("BTComposite",bt.BTNode)
bt.BTComposite = BTComposite

function BTComposite:ctor()
    bt.BTNode.ctor(self)
    self.name = "BTComposite"
    self.maxOutConnections = -1
end

