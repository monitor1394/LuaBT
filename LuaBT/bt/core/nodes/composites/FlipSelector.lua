
-----------------------------------------------------------------------------------------
-- Composites
-- FlipSelector

-- Works like a normal Selector, but when a child node returns Success, that child will 
-- be moved to the end.\n
-- As a result, previously Failed children will always be checked first and recently 
-- Successful children last
-----------------------------------------------------------------------------------------

local FlipSelector = bt.Class("FlipSelector",bt.BTComposite)
bt.FlipSelector = FlipSelector

function FlipSelector:ctor()
    bt.BTComposite.ctor(self)
    self.name = "FlipSelector"
    self.current = 1
end

function FlipSelector:init(jsonData)
end

function FlipSelector:onExecute(agent,blackboard)
    for i = self.current,#self.outConnections do
        self.status = self.outConnections[i]:execute(agent,blackboard)
        if self.status == bt.Status.Running then
            self.current = i
            return bt.Status.Running
        end
        if self.status == bt.Status.Success then
            self:sendToBack(i)
            return bt.Status.Success
        end
    end
    return bt.Status.Failure
end

function FlipSelector:sendToBack(i)
    local c = self.outConnections[i]
    table.remove( self.outConnections, i )
    table.insert( self.outConnections, c )
end

function FlipSelector:onReset()
    self.current = 1
end