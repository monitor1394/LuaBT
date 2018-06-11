
-----------------------------------------------------------------------------------------
-- Decorators
-- Optional

-- Executes the decorated node without taking into account it's return status, thus making 
-- it optional to the parent node for whether it returns Success or Failure.\n
-- This has the same effect as disabling the node, but instead it executes normaly
-----------------------------------------------------------------------------------------

local Optional = bt.Class("Optional",bt.BTDecorator)
bt.Optional = Optional
    
function Optional:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Optional"
end

function Optional:init(jsonData)
end

function Optional:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Optional
    end
    if self.status == bt.Status.Resting then
        decoratedConnection:reset(true)
    end
    self.status = decoratedConnection:execute(agent,blackboard)
    if self.status == bt.Status.Running then
        return bt.Status.Running
    else
        return bt.Status.Optional
    end
end

function Optional:destroy()
end
